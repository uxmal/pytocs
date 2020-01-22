#region License

//  Copyright 2015-2020 John Källén
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

#endregion License

using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

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
            ILexer lex = Lex(input);
            Parser par = new Parser("foo.py", lex);
            return par.test();
        }

        private List<Statement> ParseStmt(string input)
        {
            ILexer lex = Lex(input);
            Parser par = new Parser("foo.py", lex);
            return par.stmt();
        }

        private SuiteStatement ParseSuite(string input)
        {
            ILexer lex = Lex(input);
            Parser par = new Parser("foo.py", lex);
            return par.suite();
        }

        private Statement ParseFuncdef(string input)
        {
            ILexer lex = Lex(input);
            Parser par = new Parser("foo.py", lex);
            return par.funcdef()[0];
        }

        private void AssertExp(string sExp, Exp exp)
        {
            Assert.Equal(sExp, exp.ToString());
        }

        private void AssertStmt(string sExp, List<Statement> stmts)
        {
            StringBuilder sb = new StringBuilder();
            bool sep = false;
            foreach (Statement stmt in stmts)
            {
                if (sep)
                {
                    sb.AppendLine();
                }

                sb.Append(stmt);
            }

            Assert.Equal(sExp, sb.ToString());
        }

        [Fact]
        public void Lex_IdWithUnderscore()
        {
            Exp exp = ParseExp("__init__");
            AssertExp("__init__", exp);
        }

        [Fact]
        public void Parse_ArgList_TrailingComma()
        {
            Statement pyStm = ParseFuncdef("def SplitAll(operand, ): pass\r\n");
            string sExp =
                @"def SplitAll(operand):
    pass
";
            Assert.Equal(sExp, pyStm.ToString());
        }

        [Fact]
        public void Parse_AssignEmptyString()
        {
            List<Statement> pyStm = ParseStmt("for x in L : s += x\r\n");
            string sExp =
                @"for x in L:
    s += x
";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_Blank_Lines()
        {
            string pySrc =
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

            string sExp =
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
        public void Parse_Call()
        {
            string pySrc = "func(a, b=c, *d, **e)";
            string sExp = "func(a,b=c,*d,**e)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_Comment_Before_Else_Clause()
        {
            string pySrc =
                @"if foo:
    foonicate()
# wasn't foo, try bar
else:
    barnicate()
";
            string sExp =
                @"if foo:
    foonicate()
else:
    # wasn't foo, try bar
    barnicate()
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_CommentAfterElse()
        {
            string pyStm =
                @"if foo:# we have foo
  do_foo()
elif bar: # we have barness
  do_bar()
else:  # bazitude
  do_baz()
";
            string sExp =
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
        public void Parse_CommentedIf()
        {
            List<Statement> pyStm = ParseStmt(
                @"if x:
#  foo
#elif
#  bar
    foo = bar
");
            string sExp =
                @"if x:
    #  foo
    #elif
    #  bar
    foo=bar
";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_CompFor()
        {
            string pySrc = "sum(int2byte(b) for b in bytelist)";
            string sExp = "sum(int2byte(b) for b in bytelist)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_Decoration()
        {
            string pySrc =
                @"@functools.wraps(f)
def wrapper(*args, **kwargs):
    pass
";
            string sExp =
                @"@functools.wraps(f)
def wrapper(*args,**kwargs):
    pass
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_DefaultArgValue()
        {
            List<Statement> pyStm = ParseStmt("def foo(bar = baz.naz): pass\n");
            FunctionDef funcDef = (FunctionDef)pyStm[0];
            Assert.Equal("bar=baz.naz", funcDef.parameters[0].ToString());
        }

        [Fact]
        public void Parse_DottedName()
        {
            Parser parser = new Parser("foo.py", Lex("foo.bar.baz,"));
            Exp exp = parser.expr();
            AssertExp("foo.bar.baz", exp);
        }

        [Fact]
        public void Parse_EmptyPrintStatement()
        {
            List<Statement> stmt = ParseStmt("print\n");
            AssertStmt("print" + nl, stmt);
        }

        [Fact]
        public void Parse_EmptyToken()
        {
            Exp pyExp = ParseExp("()");
            Assert.Equal("()", pyExp.ToString());
        }

        [Fact]
        public void Parse_Eof()
        {
            List<Statement> pyStm = ParseStmt("return");
            AssertStmt("return\r\n", pyStm);
        }

        [Fact]
        public void Parse_EolComment()
        {
            string pySrc =
                @"def foo(bar, # continues next line
    ble, bla):
    pass
";
            string sExp =
                @"def foo(bar,ble,bla):
    pass
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Exec()
        {
            List<Statement> pyStm = ParseStmt("exec code in globals_, locals_\n");
            AssertStmt("exec code in globals_, locals_\r\n", pyStm);
        }

        [Fact]
        public void Parse_ExpressionWithString()
        {
            Exp exp = ParseExp(@"menuitem.connect(""realize"", self.on_menuitem_realize, refactoring)");
            AssertExp("menuitem.connect(\"realize\",self.on_menuitem_realize,refactoring)", exp);
        }

        [Fact]
        public void Parse_FuncdefEof()
        {
            Statement pyStm = ParseFuncdef("def foo():\n    return");
            Assert.IsAssignableFrom<FunctionDef>(pyStm);
        }

        [Fact]
        public void Parse_FunctionDef()
        {
            string pySrc =
                @"def foo(arch_options=None,
                 start=None,  # deprecated
                 end=None,  # deprecated
                 **extra_arch_options
                 ):
    return 3
";
            string sExp =
                @"def foo(arch_options=None,start=None,end=None,**extra_arch_options):
    return 3
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Id_Pos()
        {
            string pySrc = "id";
            Exp e = ParseExp(pySrc);
            Assert.Equal(0, e.Start);
            Assert.Equal(2, e.End);
        }

        [Fact]
        public void Parse_YieldFrom()
        {
            string pySrc =
                @"def foo():
    yield from bar
    yield from baz
";
            string sExp =
                @"def foo():
    yield from bar
    yield from baz
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Initializer()
        {
            List<Statement> stmt = ParseStmt(
                @"foo = [
bar(),
baz(),
]
");
            string sExp = "foo=[bar(),baz()]\r\n";
            AssertStmt(sExp, stmt);
        }

        [Fact]
        public void Parse_LambdaWithParams()
        {
            string pySrc =
                @"Base = lambda *args, **kwargs: None
";
            string sExp =
                @"Base=lambda *args,**kwargs: None
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_ListFor()
        {
            string pySrc = "[int2byte(b) for b in bytelist]";
            string sExp = "[int2byte(b) for b in bytelist]";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_ListInitializer_SingleValue()
        {
            List<Statement> pyStm = ParseStmt("a = [ 'Hello' ]\n");
            AssertStmt("a=[\"Hello\"]\r\n", pyStm);
        }

        [Fact]
        public void Parse_longInt()
        {
            List<Statement> pyStm = ParseStmt("return (1L << pos)\r\n");
            string sExp = "return (1L  <<  pos)\r\n";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_MultipleExceptClauses()
        {
            TryStatement pyStm = (TryStatement)ParseStmt(
                @"try:
    foo()
except Foo:
    a = 'f'
except Bar:
    b = 'b'
except:
    c = ''
")[0];
            Assert.Equal(3, pyStm.exHandlers.Count);
        }

        [Fact]
        public void Parse_NestedDef()
        {
            string pySrc =
                @"def foo():
    bar = 4

    " + "#" + @" inner fn
    def baz(a, b):
        print (""Bar squared"" + bar * bar)
        return False

    baz('3', 4)
";
            string sExp =
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
        public void Parse_print_to_stderr()
        {
            List<Statement> pyStm = ParseStmt("print >> sys.stderr,\"Hello\"\r\n");
            AssertStmt("print >> sys.stderr, \"Hello\"" + nl, pyStm);
        }

        [Fact]
        public void Parse_Print_TrailingComma()
        {
            List<Statement> pyStm = ParseStmt("print 'foo',\n");
            AssertStmt("print \"foo\",\r\n", pyStm);
        }

        // We do this for backwards compatability
        [Fact]
        public void Parse_PrintStatement()
        {
            List<Statement> stmt = ParseStmt("print \"Hello\"\n");
            AssertStmt("print \"Hello\"" + nl, stmt);
        }

        [Fact]
        public void Parse_Raise_ObsoleteSyntax()
        {
            List<Statement> pyStm = ParseStmt("raise AttributeError, \"widget %s not found\" % name\n");
            AssertStmt("raise AttributeError, ((\"widget %s not found\" % name),None)\r\n", pyStm);
        }

        [Fact]
        public void Parse_Regression1()
        {
            string pySrc =
                @"if ((dt.address == action.addr).model is True # FIXME: This is ugly. claripy.is_true() is the way to go
        and (dt.bits.ast == action.size.ast)):
    data_taint = dt
";
            string sExp =
                @"if (((dt.address = action.addr).model is True) and (dt.bits.ast = action.size.ast)):
    data_taint=dt
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Regression2()
        {
            string pySrc =
                @"segs = sorted(all_segments, key=lambda (_, seg): seg.offset)
";
            string sExp =
                @"segs=sorted(all_segments,key=lambda _,seg: seg.offset)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Regression3()
        {
            string pySrc =
                @"flags = ['#', '0', r'\-', r' ', r'\+', r'\'', 'I']
";
            string sExp =
                @"flags=[""#"",""0"",r""\-"",r"" "",r""\+"",r""\'"",""I""]
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_ReturnWithComment()
        {
            string pySrc =
                @"if addtup == None:
    logger.debug('DAA:  % x % x % x % x - addtup is None' % (C, H, upop, loop))
    return #FIXME: raise exception once figured out
";
            string sExp =
                @"if (addtup = None):
    logger.debug((""DAA:  % x % x % x % x - addtup is None"" % (C,H,upop,loop)))
    return
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Set()
        {
            string pySrc = "{self._path_merge_points[addr]}";
            Exp e = ParseExp(pySrc);
            AssertExp("{ self._path_merge_points[addr] }", e);
        }

        [Fact]
        public void Parse_SetBuilder()
        {
            List<Statement> pyExpr = ParseStmt(
                @"r = {
    'major'  : '2',
    'minor'  : '7',
}
");
            AssertStmt("r={ \"major\" : \"2\", \"minor\" : \"7\",  }\r\n", pyExpr);
        }

        [Fact]
        public void Parse_SetComprehension()
        {
            string pySrc = "{ id(e) for e in self._breakpoints[t] }";
            string sExp = "{id(e) for e in self._breakpoints[t]}";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_SetNamedArgumentValue()
        {
            string pySrc =
                @"def print_no_end(text):
    print(text, end = '')
";
            string sExp =
                @"def print_no_end(text):
    print text, end=""""
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Shift()
        {
            List<Statement> pyStm = ParseStmt("return bit >> BitSet.LOG_BITS\r\n");
            string sExp = "return (bit  >>  BitSet.LOG_BITS)\r\n";
            AssertStmt(sExp, pyStm);
        }

        [Fact]
        public void Parse_Slice()
        {
            string pySrc = "a[::]";
            Exp e = ParseExp(pySrc);
            AssertExp("a[::]", e);
        }

        [Fact]
        public void Parse_StaggeredComment()
        {
            FunctionDef pyStm = (FunctionDef)ParseFuncdef("def x():\n  version = 1\n  #foo\n    #bar\n");
            Assert.Equal("version=1\r\n#foo\r\n#bar\r\n", pyStm.body.ToString());
        }

        [Fact]
        public void Parse_Test()
        {
            string pySrc = "x if foo else y";
            string sExp = "x if foo else y";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parse_Trailing_Comment()
        {
            string pySrc =
                @"if foo:
    stack.append(bar)

   #subgraph = stack
    subgraph = None
";
            string sExp =
                @"if foo:
    stack.append(bar)
    #subgraph = stack
    subgraph=None
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_Trailing_Comments_After_If()
        {
            string pySrc =
                @"def test():
    if foo:
        foonicate()
    # wasn't foo, continue
";
            string sExp =
                @"def test():
    if foo:
        foonicate()
        # wasn't foo, continue

";
            Statement pyStm = ParseFuncdef(pySrc);
            Assert.Equal(sExp, pyStm.ToString());
        }

        [Fact]
        public void Parse_Trailing_Tuple()
        {
            string pySrc =
                @"class bar:
    def foo():
        code()

        return 1,2,3,4
";
            string sExp =
                @"class bar:
    def foo():
        code()
        return 1,2,3,4

";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_TryWithComments()
        {
            List<Statement> pyStm = ParseStmt(
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

            string sExp =
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
        public void Parse_TupleArguments()
        {
            string pySrc =
                @"def foo(self, (value, sort)):
    self.value = value
";
            string sExp =
                @"def foo(self,(value,sort)):
    self.value=value
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parse_With()
        {
            string pySrc =
                @"with foo():
    bar()
";
            string sExp =
                @"with foo():
    bar()
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_adjacent_string_constants()
        {
            string pySrc = @"(
    'prefix'    # prefix
    'suffix'    # suffix
)";
            string sExp = "\"prefixsuffix\"";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parser_AndExp_Comment()
        {
            string pySrc = @"
if (
    condition1
    and
    # Comment here => ""not"" is Unexpected
    not condition2
):
    return early
return late
";
            string sExp =
                @"if (condition1 and not condition2):
    return early
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_async_await()
        {
            string pySrc =
                @"async def fnordAsync():
    await asyncio.sleep(1)
";
            string sExp =
                @"async def fnordAsync():
    await asyncio.sleep(1)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_BitwiseComplement))]
        public void Parser_BitwiseComplement()
        {
            string pySrc =
                @"a = ExprCond(magn1,
    # magn1 == magn2, are the signal equals?
    ~(sign1 ^ sign2))";
            string sExp = "a=ExprCond(magn1,~(sign1 ^ sign2))" + Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }

        // Reported in Github 26
        [Fact]
        public void Parser_complex()
        {
            string pySrc = @"3 + 2j";
            string sExp = @"(3  +  2j)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parser_decorator_trailing_comment()
        {
            string pySrc = @"
@decorator   #trailing comment
def foo():
    pass
";
            string sExp =
                @"@decorator()
def foo():
    pass
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_DeeplyNestedStatementFollowedByComment()
        {
            string pySrc =
                @"class foo:
    def bar():
        for i in blox:
            blah(i)
    # next method
    def next():
        pass
";
            string sExp =
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
        public void Parser_Dictionary_unpacker()
        {
            string pySrc =
                @"return TestValue(
    {
        **foo,
        **bar
    }
)";
            string sExp =
                @"return TestValue({ **foo, **bar,  })
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        // Reported in GitHub issue 29
        [Fact]
        public void Parser_funcdef_excess_positionalParameters()
        {
            string pySrc =
                @"def foo(*args):
    return len(args)
";
            string sExp =
                @"def foo(*args):
    return len(args)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_Import_commented()
        {
            string pySrc =
                @"from utils import (
    # foo
    # bar
    baz,)
";
            string sExp =
                @"from utils import (baz)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_Import_commented2()
        {
            string pySrc =
                @"from utils import (
    foo,
    # bar
    baz,)
";
            string sExp =
                @"from utils import (foo, baz)
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        // Reported in Github 26
        [Fact]
        public void Parser_Infinity()
        {
            string pySrc =
                @"PosInf = float('+inf')
";
            string sExp =
                @"PosInf=float(""+inf"")
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_issue_57))]
        public void Parser_issue_57()
        {
            string pySrc = "{'a': 'str', **kwargs }";
            string sExp = @"{ ""a"" : ""str"", **kwargs,  }";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_issue_61))]
        public void Parser_issue_61()
        {
            string pySrc =
                @"class TestClass:
    def TestFunction(self):
        return TestValue(
            {
                **something
            }
        )";
            string sExp =
                @"class TestClass:
    def TestFunction(self):
        return TestValue({ **something,  })

";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_lambda_kwargs()
        {
            string pySrc = "lambda x, **k: xpath_text(hd_doc, './/video/' + x, **k)";
            string sExp = "lambda x,**k: xpath_text(hd_doc,(\".//video/\"  +  x),**k)";
            AssertExp(sExp, ParseExp(pySrc));
        }

        [Fact]
        public void Parser_list_initializer_with_comment()
        {
            string pySrc =
                @"foo = [
    # empty
]";
            string sExp =
                @"foo=[]
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_List_unpacker()
        {
            string pySrc =
                @"return TestValue(
    [
        *foo,
        *bar
    ]
)";
            string sExp =
                @"return TestValue([*foo,*bar])
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_ListComprehension_Alternating_fors))]
        public void Parser_ListComprehension_Alternating_fors()
        {
            string pySrc =
                "states = [state for (stash, states) in self.simgr.stashes.items() if stash != 'pruned' for state in states]\n";
            string sExp =
                "states=[state for (stash,states) in self.simgr.stashes.items() if (stash  !=  \"pruned\") for state in states]" +
                Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_print_trailing_comma))]
        public void Parser_print_trailing_comma()
        {
            string pySrc =
                @"print('foo:'),
";
            string sExp = "print \"foo:\"," + Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact]
        public void Parser_Set_unpacker()
        {
            string pySrc =
                @"return TestValue(
    {
        *foo,
        *bar
    }
)";
            string sExp =
                @"return TestValue({ *foo, *bar })
";
            AssertStmt(sExp, ParseStmt(pySrc));
        }

        [Fact(DisplayName = nameof(Parser_VariableAnnotation))]
        public void Parser_VariableAnnotation()
        {
            string pySrc = "ints: List[int] = []";
            string sExp = "ints: List[int]=[]" + Environment.NewLine;

            AssertStmt(sExp, ParseStmt(pySrc));
        }
    }
}