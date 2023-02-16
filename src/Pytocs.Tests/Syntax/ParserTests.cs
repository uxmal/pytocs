#region License
//  Copyright 2015-2021 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.Syntax;

namespace Pytocs.UnitTests.Syntax
{
    public class ParserTests
    {
        private static readonly string nl = Environment.NewLine;

        private ILexer Lex(string input)
        {
            return new CommentFilter(
                new Lexer(
                    "foo.py",
                    new StringReader(input)));
        }

        private Exp ParseExp(string input)
        {
            var lex = Lex(input);
            var par = new Parser("foo.py", lex);
            return par.test()!;
        }

        private List<Statement> ParseStmt(string input)
        {
            var lex = Lex(input);
            var par = new Parser("foo.py", lex);
            return par.stmt();
        }

        private SuiteStatement ParseSuite(string input)
        {
            var lex = Lex(input);
            var par = new Parser("foo.py", lex);
            return par.suite();
        }

        private Statement ParseFuncdef(string input)
        {
            var lex = Lex(input);
            var par = new Parser("foo.py", lex);
            return par.funcdef()[0];
        }

        private void AssertExp(string sExp, Exp exp)
        {
            Assert.Equal(sExp, exp.ToString());
        }

        private void AssertStmt(string sExp, List<Statement> stmts)
        {
            var sb = new StringBuilder();
            var sep = false;
            foreach (var stmt in stmts)
            {
                if (sep)
                    sb.AppendLine();
                sb.Append(stmt);
            }
            Assert.Equal(sExp, sb.ToString());
        }

        [Fact]
        public void Parse_DottedName()
        {
            var parser = new Parser("foo.py", Lex("foo.bar.baz,"));
            var exp = parser.expr()!;
            AssertExp("foo.bar.baz", exp);
        }

        [Fact]
        public void Parse_ExpressionWithString()
        {
            var exp = ParseExp(@"menuitem.connect(""realize"", self.on_menuitem_realize, refactoring)");
            AssertExp("menuitem.connect(\"realize\",self.on_menuitem_realize,refactoring)", exp);
        }

        // We do this for backwards compatability
        [Fact]
        public void Parse_PrintStatement()
        {
            var stmt = ParseStmt("print \"Hello\"\n");
            AssertStmt("print \"Hello\"" + nl, stmt);
        }

        [Fact]
        public void Parse_EmptyPrintStatement()
        {
            var stmt = ParseStmt("print\n");
            AssertStmt("print" + nl, stmt);
        }

        [Fact]
        public void Parse_Initializer()
        {
            var stmt = ParseStmt(
@"foo = [
bar(),
baz(),
]
");
            var sExp = "foo=[bar(),baz()]\r\n";
            AssertStmt(sExp, stmt);
        }

        [Fact]
        public void Lex_IdWithUnderscore()
        {
            var exp = ParseExp("__init__");
            AssertExp("__init__", exp);
        }

        [Fact]
        public void Parse_SetBuilder()
        {
            var pyExpr = ParseStmt(
@"r = {
    'major'  : '2',
    'minor'  : '7',   
}
");
            AssertStmt("r={ \"major\" : \"2\", \"minor\" : \"7\",  }\r\n", pyExpr);
        }

        [Fact]
        public void Parse_AssignEmptyString()
        {
            var pyStm = ParseStmt("for x in L : s += x\r\n");
            var sExp =
@"for x in L:
    s += x
";
            AssertStmt(sExp, pyStm);

        }

        [Fact]
        public void Parse_Shift()
        {
            var pyStm = ParseStmt("return bit >> BitSet.LOG_BITS\r\n");
            var sExp = "return (bit  >>  BitSet.LOG_BITS)\r\n";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_longInt()
        {
            var pyStm = ParseStmt("return (1L << pos)\r\n");
            var sExp = "return (1L  <<  pos)\r\n";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_print_to_stderr()
        {
            var pyStm = ParseStmt("print >> sys.stderr,\"Hello\"\r\n");
            AssertStmt("print >> sys.stderr, \"Hello\"" + nl, pyStm);
        }

        [Fact]
        public void Parse_EmptyToken()
        {
            var pyExp = ParseExp("()");
            Assert.Equal("()", pyExp.ToString());
        }

        [Fact]
        public void Parse_Eof()
        {
            var pyStm = ParseStmt("return");
            AssertStmt("return\r\n", pyStm);
        }

        [Fact]
        public void Parse_FuncdefEof()
        {
            var pyStm = ParseFuncdef("def foo():\n    return");
            Assert.IsAssignableFrom<FunctionDef>(pyStm);
        }

        [Fact]
        public void Parse_MultipleExceptClauses()
        {
            var pyStm = (TryStatement) ParseStmt(
@"try:
    foo()
except Foo:
    a = 'f'
except Bar:
    b = 'b'
except:
    c = ''
")[0];
            Assert.Equal(3, pyStm.ExHandlers.Count);
        }

        [Fact]
        public void Parse_Raise_ObsoleteSyntax()
        {
            var pyStm = ParseStmt("raise AttributeError, \"widget %s not found\" % name\n");
            AssertStmt("raise AttributeError, ((\"widget %s not found\" % name),None)\r\n", pyStm);
        }

        [Fact]
        public void Parse_Print_TrailingComma()
        {
            var pyStm = ParseStmt("print 'foo',\n");
            AssertStmt("print \"foo\",\r\n", pyStm);
        }

        [Fact]
        public void Parse_ArgList_TrailingComma()
        {
            var pyStm = ParseFuncdef("def SplitAll(operand, ): pass\r\n");
            var sExp =
@"def SplitAll(operand):
    pass
";
            Assert.Equal(sExp, pyStm.ToString());
        }

        [Fact(Skip = "This is a Python 2-ism")]
        public void Parse_Exec()
        {
            var pyStm = ParseStmt("exec code in globals_, locals_\n");
            AssertStmt("exec code in globals_, locals_\r\n", pyStm);
        }

        [Fact]
        public void Parse_DefaultArgValue()
        {
            var pyStm = ParseStmt("def foo(bar = baz.naz): pass\n");
            var funcDef = (FunctionDef) pyStm[0];
            Assert.Equal("bar=baz.naz", funcDef.parameters[0].ToString());
        }

        [Fact]
        public void Parse_ListInitializer_SingleValue()
        {
            var pyStm = ParseStmt("a = [ 'Hello' ]\n");
            AssertStmt("a=[\"Hello\"]\r\n", pyStm);
        }

        [Fact]
        public void Parse_StaggeredComment()
        {
            var pyStm = (FunctionDef) ParseFuncdef("def x():\n  version = 1\n  #foo\n    #bar\n");
            Assert.Equal("version=1\r\n#foo\r\n#bar\r\n", pyStm.body.ToString());
        }

        [Fact]
        public void Parse_CommentedIf()
        {
            var pyStm = ParseStmt(
@"if x:
#  foo
#elif
#  bar
    foo = bar
");
            var sExp =
@"if x:
    #  foo
    #elif
    #  bar
    foo=bar
";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_TryWithComments()
        {
            var pyStm = ParseStmt(
@"try:
    if self._returnToken:
        raise antlr.TryAgain ### found SKIP token
    ### option { testLiterals=true }
    self.testForLiteral(self._returnToken)
    ### return token to caller
    return self._returnToken
    ### handle lexical errors ....
except antlr.RecognitionException, e:
    raise hell
");

            var sExp =
@"try:
    if self._returnToken:
        raise antlr.TryAgain
    
    ### option { testLiterals=true }
    self.testForLiteral(self._returnToken)
    ### return token to caller
    return self._returnToken
    ### handle lexical errors ....
except antlr.RecognitionException as e:
    raise hell
";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_CommentAfterElse()
        {
            var pyStm =
@"if foo:# we have foo
  do_foo()
elif bar: # we have barness
  do_bar()
else:  # bazitude
  do_baz()
";
            var sExp =
@"if foo:
    # we have foo
    do_foo()
elif bar:
    # we have barness
    do_bar()
else:
    # bazitude
    do_baz()
";
            AssertStmt(sExp, ParseStmt(pyStm));
        }

        [Fact]
        public void Parse_ListFor()
        {
            var pySrc = "[int2byte(b) for b in bytelist]";
            var sExp = "[int2byte(b) for b in bytelist]";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_CompFor()
        {
            var pySrc = "sum(int2byte(b) for b in bytelist)";
            var sExp = "sum(int2byte(b) for b in bytelist)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_Test()
        {
            var pySrc = "x if foo else y";
            var sExp = "x if foo else y";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_Decoration()
        {
            var pySrc =
@"@functools.wraps(f)
def wrapper(*args, **kwargs):
    pass
";
            var sExp =
@"@functools.wraps(f)
def wrapper(*args,**kwargs):
    pass
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_With()
        {
            var pySrc =
@"with foo():
    bar()
";
            var sExp =
@"with foo():
    bar()
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_YieldFrom()
        {
            var pySrc =
@"def foo():
    yield from bar
    yield from baz
";
            var sExp =
@"def foo():
    yield from bar
    yield from baz
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Call()
        {
            var pySrc = "func(a, b=c, *d, **e)";
            var sExp = "func(a,b=c,*d,**e)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_Id_Pos()
        {
            var pySrc = "id";
            var e = ParseExp(pySrc);
            Assert.Equal(0, e.Start);
            Assert.Equal(2, e.End);
        }

        [Fact]
        public void Parse_Set()
        {
            var pySrc = "{self._path_merge_points[addr]}";
            var e = ParseExp(pySrc);
            AssertExp("{ self._path_merge_points[addr] }", e);
        }

        [Fact]
        public void Parse_Slice()
        {
            var pySrc = "a[::]";
            var e = ParseExp(pySrc);
            AssertExp("a[:]", e);
        }

        [Fact]
        public void Parse_Regression1()
        {
            var pySrc =
@"if ((dt.address == action.addr).model is True # FIXME: This is ugly. claripy.is_true() is the way to go
        and (dt.bits.ast == action.size.ast)):
    data_taint = dt
";
            var sExp =
@"if (((dt.address = action.addr).model is True) and (dt.bits.ast = action.size.ast)):
    data_taint=dt
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Regression2()
        {
            var pySrc =
@"segs = sorted(all_segments, key=lambda (_, seg): seg.offset)
";
            var sExp =
@"segs=sorted(all_segments,key=lambda _,seg: seg.offset)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Regression3()
        {
            var pySrc =
@"flags = ['#', '0', r'\-', r' ', r'\+', r'\'', 'I']
";
            var sExp =
@"flags=[""#"",""0"",r""\-"",r"" "",r""\+"",r""\'"",""I""]
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_SetComprehension()
        {
            var pySrc = "{ id(e) for e in self._breakpoints[t] }";
            var sExp = "{id(e) for e in self._breakpoints[t]}";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_TupleArguments()
        {
            var pySrc =
@"def foo(self, (value, sort)):
    self.value = value
";
            var sExp =
@"def foo(self,(value,sort)):
    self.value=value
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_LambdaWithParams()
        {
            var pySrc =
@"Base = lambda *args, **kwargs: None
";
            var sExp =
@"Base=lambda *args,**kwargs: None
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_EolComment()
        {
            var pySrc =
@"def foo(bar, # continues next line
    ble, bla):
    pass
";
            var sExp =
@"def foo(bar,ble,bla):
    pass
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_ReturnWithComment()
        {
            var pySrc =
@"if addtup == None:
    logger.debug('DAA:  % x % x % x % x - addtup is None' % (C, H, upop, loop))
    return #FIXME: raise exception once figured out
";
            var sExp =
@"if (addtup = None):
    logger.debug((""DAA:  % x % x % x % x - addtup is None"" % (C,H,upop,loop)))
    return
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_FunctionDef()
        {
            var pySrc =
@"def foo(arch_options=None,
                 start=None,  # deprecated
                 end=None,  # deprecated
                 **extra_arch_options
                 ):
    return 3
";
            var sExp =
@"def foo(arch_options=None,start=None,end=None,**extra_arch_options):
    return 3
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_SetNamedArgumentValue()
        {
            var pySrc =
@"def print_no_end(text):
    print(text, end = '')
";
            var sExp =
@"def print_no_end(text):
    print text, end=""""
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Comment_Before_Else_Clause()
        {
            var pySrc =
@"if foo:
    foonicate()
# wasn't foo, try bar
else:
    barnicate()
";
            var sExp =
@"if foo:
    foonicate()
else:
    # wasn't foo, try bar
    barnicate()
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Trailing_Comments_After_If()
        {
            var pySrc =
@"def test():
    if foo:
        foonicate()
    # wasn't foo, continue
";
            var sExp =
@"def test():
    if foo:
        foonicate()
        # wasn't foo, continue
    
";
            var pyStm = ParseFuncdef(pySrc);
            Assert.Equal(sExp, pyStm.ToString());
        }

        [Fact]
        public void Parse_NestedDef()
        {
            var pySrc =
@"def foo():
    bar = 4

    " + "#" + @" inner fn
    def baz(a, b):
        print (""Bar squared"" + bar * bar)
        return False

    baz('3', 4)
";
            var sExp =
@"def foo():
    bar=4
    " + "#" + @" inner fn
    def baz(a,b):
        print (""Bar squared""  +  (bar  *  bar))
        return False
    
    baz(""3"",4)
";
            AssertStmt(sExp, ParseStmt(pySrc));

        }

        [Fact]
        public void Parse_Blank_Lines()
        {
            var pySrc =
@"def get_whitelisted_statements(blob, addr):
	""""""
	:returns: True if all statements are whitelisted
	""""""
	if addr in blob._run_statement_whitelist:
		if blob._run_statement_whitelist[addr] is True:
			return None # This is the default value used to say
						# we execute all statements in this basic block. A
						# little weird...
	
		else:
			return blob._run_statement_whitelist[addr]

	else:
		return []";

            var sExp =
@"def get_whitelisted_statements(blob,addr):
    ""
	:returns: True if all statements are whitelisted
	""
    if (addr in blob._run_statement_whitelist):
        if (blob._run_statement_whitelist[addr] is True):
            return None
            # we execute all statements in this basic block. A
            # little weird...
        else:
            return blob._run_statement_whitelist[addr]
        
    else:
        return []
    
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Trailing_Comment()
        {
            var pySrc =
@"if foo:
    stack.append(bar)

   #subgraph = stack
    subgraph = None
";
            var sExp =
@"if foo:
    stack.append(bar)
    #subgraph = stack
    subgraph=None
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Trailing_Tuple()
        {
            var pySrc =
@"class bar:
    def foo():
        code()

        return 1,2,3,4
";
            var sExp =
@"class bar:
    def foo():
        code()
        return 1,2,3,4
    
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_DeeplyNestedStatementFollowedByComment()
        {
            var pySrc =
@"class foo:
    def bar():
        for i in blox:
            blah(i)
    # next method
    def next():
        pass
";
            var sExp =
@"class foo:
    def bar():
        for i in blox:
            blah(i)
        
    
    # next method
    def next():
        pass
    
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_ArrayComprehension()
        {
            var pySrc =
                "[state for (stash, states) in self.simgr.stashes.items() if (stash != 'pruned') for state in states ]";
            var sExp =
                "[state for (stash,states) in self.simgr.stashes.items() if (stash  !=  \"pruned\") for state in states]";
            var exp = ParseExp(pySrc);
            AssertExp(sExp, exp);
        }

        // Reported in Github 26
        [Fact]
        public void Parser_Infinity()
        {
            var pySrc =
@"PosInf = float('+inf')
";
            var sExp =
@"PosInf=float(""+inf"")
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        // Reported in Github 26
        [Fact]
        public void Parser_complex()
        {
            var pySrc = @"3 + 2j";
            var sExp = @"(3  +  2j)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parser_async_await()
        {
            var pySrc =
@"async def fnordAsync():
    await asyncio.sleep(1)
";
            var sExp =
@"async def fnordAsync():
    await asyncio.sleep(1)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        // Reported in GitHub issue 29
        [Fact]
        public void Parser_funcdef_excess_positionalParameters()
        {
            var pySrc =
@"def foo(*args):
    return len(args)
";
            var sExp =
@"def foo(*args):
    return len(args)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_ListComprehension_Alternating_fors))]
        public void Parser_ListComprehension_Alternating_fors()
        {
            var pySrc = "states = [state for (stash, states) in self.simgr.stashes.items() if stash != 'pruned' for state in states]\n";
            var sExp = "states=[state for (stash,states) in self.simgr.stashes.items() if (stash  !=  \"pruned\") for state in states]" + Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_VariableAnnotation))]
        public void Parser_VariableAnnotation()
        {
            var pySrc = "ints: List[int] = []";
            var sExp = "ints: List[int]=[]" + Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_BitwiseComplement))]
        public void Parser_BitwiseComplement()
        {
            var pySrc =
@"a = ExprCond(magn1,
    # magn1 == magn2, are the signal equals?
    ~(sign1 ^ sign2))";
            var sExp = "a=ExprCond(magn1,~(sign1 ^ sign2))" + Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }


        [Fact(DisplayName = nameof(Parser_print_trailing_comma))]
        public void Parser_print_trailing_comma()
        {
            var pySrc =
@"print('foo:'),
";
            var sExp = "print \"foo:\"," + Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_issue_57))]
        public void Parser_issue_57()
        {
            var pySrc = "{'a': 'str', **kwargs }";
            var sExp = @"{ ""a"" : ""str"", **kwargs,  }";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_issue_61))]
        public void Parser_issue_61()
        {
            var pySrc =
@"class TestClass:
    def TestFunction(self):
        return TestValue(
            {
                **something
            }
        )";
            var sExp =
@"class TestClass:
    def TestFunction(self):
        return TestValue({ **something,  })
    
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_Import_commented()
        {
            var pySrc =
@"from utils import (
    # foo
    # bar 
    baz,)
";
            var sExp =
@"from utils import (baz)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_Import_commented2()
        {
            var pySrc =
@"from utils import (
    foo,
    # bar 
    baz,)
";
            var sExp =
@"from utils import (foo, baz)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_lambda_kwargs()
        {
            var pySrc = "lambda x, **k: xpath_text(hd_doc, './/video/' + x, **k)";
            var sExp = "lambda x,**k: xpath_text(hd_doc,(\".//video/\"  +  x),**k)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parser_Dictionary_unpacker()
        {
            var pySrc =
@"return TestValue(
    {
        **foo,
        **bar
    }
)";
            var sExp =
@"return TestValue({ **foo, **bar,  })
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_AndExp_Comment()
        {
            var pySrc = @"
if (
    condition1
    and
    # Comment here => ""not"" is Unexpected
    not condition2
):
    return early
return late
";
            var sExp =
@"if (condition1 and not condition2):
    return early
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_Set_unpacker()
        {
            var pySrc =
@"return TestValue(
    {
        *foo,
        *bar
    }
)";
            var sExp =
@"return TestValue({ *foo, *bar })
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_List_unpacker()
        {
            var pySrc =
@"return TestValue(
    [
        *foo,
        *bar
    ]
)";
            var sExp =
@"return TestValue([*foo,*bar])
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_list_initializer_with_comment()
        {
            var pySrc =
@"foo = [
    # empty
]";
            var sExp =
@"foo=[]
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_adjacent_string_constants()
        {
            var pySrc = @"(
    'prefix'    # prefix
    'suffix'    # suffix
)";
            var sExp = "\"prefixsuffix\"";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parser_decorator_trailing_comment()
        {
            var pySrc = @"
@decorator   #trailing comment
def foo():
    pass
";
            var sExp =
@"@decorator()
def foo():
    pass
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_return_UnaryTuple()
        {
            var pySrc = @"
return x.foo,
";
            var sExp = @"return x.foo,
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_FunctionDefAnnotation()
        {
            var pySrc = @"
def func(arg1: int, arg2: str) -> str:
    return 'Hi'
";
            var sExp =
@"def func(arg1: int,arg2: str) -> str:
    return ""Hi""
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_Multiline_FuncDef_Trailing_Comma))]
        public void Parser_Multiline_FuncDef_Trailing_Comma()
        {
            var pySrc = @"
def __init__(
        self,
        aux_data={},  # type: DictLike[str, AuxData]
        uuid=None,  # type: typing.Optional[UUID]
    ):
    pass
";
            var sExp = 
@"def @__init__(self,aux_data={  },uuid=None):
    pass
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_AssignmentExpression))]
        public void Parser_AssignmentExpression()
        {
            var pySrc = @"
while chunk := read(256):
    process(chunk)
";
            var sExp =
@"while chunk := read(256):
    process(chunk)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_Assign_Assign))]
        public void Parser_Assign_Assign()
        {
            var pySrc = @"
x = y = z = func(w)
";
            var sExpected =
@"x=y=z=func(w)
";
            AssertStmt(sExpected, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_Github_89))]
        public void Parser_Github_89()
        {
            var pySrc = @"
def device_readstb(self, flags, io_timeout):
    if self.srq_active:
        stb |= 0b_0100_0000
    return error, stb
";
            var sExpected =
@"def device_readstb(self,flags,io_timeout):
    if self.srq_active:
        stb |= 0b_0100_0000
    
    return error,stb
";
            AssertStmt(sExpected, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_matmul))]
        public void Parser_matmul()
        {
            var pySrc = @"a @ b";
            AssertExp("(a @ b)", ParseExp(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_aug_matmul))]
        public void Parser_aug_matmul()
        {
            var pySrc = @"a @= b";
            var sExpected =
                @"a @= b
";
            AssertStmt(sExpected, ParseStmt(pySrc));
        }
    }
}
