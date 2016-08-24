#region License
//  Copyright 2015 John Källén
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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Syntax
{
    [TestFixture]
    public class ParserTests
    {
        private static readonly string nl = Environment.NewLine;

        private Lexer Lex(string input)
        {
            return new Lexer("foo.py", new StringReader(input));
        }

        private Exp ParseExp(string input)
        {
            var lex = Lex(input);
            var par = new Parser("foo.py", lex);
            return par.test();
        }

        private Statement ParseStmt(string input)
        {
            var lex = Lex(input);
            var par = new Parser("foo.py", lex);
            return par.stmt();
        }

        private Statement ParseSuite(string input)
        {
            var lex = Lex(input);
            lex.Get();
            var par = new Parser("foo.py", lex);
            return par.suite();
        }

        private Statement ParseFuncdef(string input)
        {
            var lex = Lex(input);
            var par = new Parser("foo.py", lex);
            return par.funcdef();
        }

        [Test]
        public void Parse_DottedName()
        {
            var parser = new Parser("foo.py", Lex("foo.bar.baz,"));
            var exp = parser.expr();
            Assert.AreEqual("foo.bar.baz", exp.ToString());
        }

        [Test]
        public void Parse_ExpressionWithString()
        {
            var exp = ParseExp(@"menuitem.connect(""realize"", self.on_menuitem_realize, refactoring)");
            Assert.AreEqual("menuitem.connect(\"realize\",self.on_menuitem_realize,refactoring)", exp.ToString());
        }

        [Test(Description = "We do this for backwards compatability")]
        public void Parse_PrintStatement()
        {
            var stmt = ParseStmt("print \"Hello\"\n");
            Assert.AreEqual("print \"Hello\"" + nl, stmt.ToString());
        }

        [Test]
        public void Parse_EmptyPrintStatement()
        {
            var stmt = ParseStmt("print\n");
            Assert.AreEqual("print" + nl, stmt.ToString());
        }

        [Test]
        public void Parse_Initializer()
        {
            var stmt = ParseStmt(
@"foo = [
bar(),
baz(),
]
");
            var sExp = "foo=[bar(),baz()]\r\n";
            Assert.AreEqual(sExp, stmt.ToString());
        }

        [Test]
        public void Lex_IdWithUnderscore()
        {
            var exp = ParseExp("__init__");
            Assert.AreEqual("__init__", exp.ToString());
        }

        [Test]
        public void Parse_SetBuilder()
        {
            var pyExpr = ParseStmt(
@"r = {
    'major'  : '2',
    'minor'  : '7',   
}
");
            Assert.AreEqual("r={ \"major\" : \"2\", \"minor\" : \"7\",  }\r\n", pyExpr.ToString());
        }

        [Test]
        public void Parse_AssignEmptyString()
        {
            var pyStm = ParseStmt("for x in L : s += x\r\n");
            var sExp =
@"for x in L:
    s += x
";
            Assert.AreEqual(sExp, pyStm.ToString());

        }

        [Test]
        public void Parse_Shift()
        {
            var pyStm = ParseStmt("return bit >> BitSet.LOG_BITS\r\n");
            var sExp = "return (bit  >>  BitSet.LOG_BITS)\r\n";
            Assert.AreEqual(sExp, pyStm.ToString());
        }

        [Test]
        public void Parse_longInt()
        {
            var pyStm = ParseStmt("return (1L << pos)\r\n");
            var sExp = "return (1L  <<  pos)\r\n";
            Assert.AreEqual(sExp, pyStm.ToString());
        }

        [Test]
        public void Parse_print_to_stderr()
        {
            var pyStm = ParseStmt("print >> sys.stderr,\"Hello\"\r\n");
            Assert.AreEqual("print >> sys.stderr, \"Hello\""+nl, pyStm.ToString());
        }

        [Test]
        public void Parse_EmptyToken()
        {
            var pyExp = ParseExp("()");
            Assert.AreEqual("()", pyExp.ToString());
        }

        [Test]
        public void Parse_Eof()
        {
            var pyStm = ParseStmt("return");
            Assert.AreEqual("return\r\n", pyStm.ToString());
        }

        [Test]
        public void Parse_FuncdefEof()
        {
            var pyStm = ParseFuncdef("def foo():\n    return");
            Assert.IsInstanceOf<FunctionDef>(pyStm);
        }

        [Test]
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
");
            Assert.AreEqual(3, pyStm.exHandlers.Count);
        }

        [Test]
        public void Parse_Raise_ObsoleteSyntax()
        {
            var pyStm = ParseStmt("raise AttributeError, \"widget %s not found\" % name\n");
            Assert.AreEqual("raise AttributeError, ((\"widget %s not found\" % name),None)\r\n", pyStm.ToString());
        }

        [Test]
        public void Parse_Print_TrailingComma()
        {
            var pyStm = ParseStmt("print 'foo',\n");
            Assert.AreEqual("print \"foo\",\r\n", pyStm.ToString());
        }

        [Test]
        public void Parse_ArgList_TrailingComma()
        {
            var pyStm = ParseFuncdef("def SplitAll(operand, ): pass\r\n");
            var sExp =
@"def SplitAll(operand):
    pass
";
            Assert.AreEqual(sExp, pyStm.ToString());
        }

        [Test]
        public void Parse_Exec()
        {
            var pyStm = ParseStmt("exec code in globals_, locals_\n");
            Assert.AreEqual("exec code in globals_, locals_\r\n", pyStm.ToString());
        }

        [Test]
        public void Parse_DefaultArgValue()
        {
            var pyStm = ParseStmt("def foo(bar = baz.naz): pass\n");
            var funcDef = (FunctionDef) pyStm;
            Assert.AreEqual("bar=baz.naz", funcDef.parameters[0].ToString());
        }

        [Test]
        public void Parse_ListInitializer_SingleValue()
        {
            var pyStm = ParseStmt("a = [ 'Hello' ]\n");
            Assert.AreEqual("a=[\"Hello\"]\r\n", pyStm.ToString());
        }

        [Test]
        public void Parse_StaggeredComment()
        {
            var pyStm = (FunctionDef) ParseFuncdef("def x():\n  version = 1\n  #foo\n    #bar\n");
            Assert.AreEqual("version=1\r\n#foo\r\n#bar\r\n", pyStm.body.ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, pyStm.ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, pyStm.ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, ParseStmt(pyStm).ToString());
        }

        [Test]
        public void Parse_ListFor()
        {
            var pySrc = "[int2byte(b) for b in bytelist]";
            var sExp = "[int2byte(b) for b in bytelist]";
            Assert.AreEqual(sExp, ParseExp(pySrc).ToString());
        }

        [Test]
        public void Parse_CompFor()
        {
            var pySrc = "sum(int2byte(b) for b in bytelist)";
            var sExp = "sum(int2byte(b) for b in bytelist)";
            Assert.AreEqual(sExp, ParseExp(pySrc).ToString());
        }

        [Test]
        public void Parse_Test()
        {
            var pySrc = "x if foo else y";
            var sExp = "x if foo else y";
            Assert.AreEqual(sExp, ParseExp(pySrc).ToString());
        }

        [Test]
        public void Parse_Decoration()
        {
            var pySrc =
@"@functools.wraps(f)
def wrapper(*args, **kwargs):
    pass
";
            var sExp =
@"@functools.wraps(f)
def wrapper():
    pass
";
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
        public void Parse_Call()
        {
            var pySrc = "func(a, b=c, *d, **e)";
            var sExp = "func(a,b=c,*d,**e)";
            Assert.AreEqual(sExp, ParseExp(pySrc).ToString());
        }

        [Test]
        public void Parse_Id_Pos()
        {
            var pySrc = "id";
            var e = ParseExp(pySrc);
            Assert.AreEqual(0, e.Start);
            Assert.AreEqual(2, e.End);
        }

        [Test]
        public void Parse_Set()
        {
            var pySrc = "{self._path_merge_points[addr]}";
            var e = ParseExp(pySrc);
            Assert.AreEqual("{ self._path_merge_points[addr] }", e.ToString());
        }

        [Test]
        public void Parse_Slice()
        {
            var pySrc = "a[::]";
            var e = ParseExp(pySrc);
            Assert.AreEqual("a[::]", e.ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
        public void Parse_Regression2()
        {
            var pySrc = 
@"segs = sorted(all_segments, key=lambda (_, seg): seg.offset)
";
            var sExp =
@"segs=sorted(all_segments,key=lambda (_,seg): seg.offset)
"; 
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
        public void Parse_Regression3()
        {
            var pySrc = 
@"flags = ['#', '0', r'\-', r' ', r'\+', r'\'', 'I']
";
            var sExp =
@"flags=[""#"",""0"",r""\-"",r"" "",r""\+"",r""\'"",""I""]
";
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
        public void Parse_SetComprehension()
        {
            var pySrc = "{ id(e) for e in self._breakpoints[t] }";
            var sExp = "{id(e) for e in self._breakpoints[t]}";
            Assert.AreEqual(sExp, ParseExp(pySrc).ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
        public void Parse_LambdaWithParams()
        {
            var pySrc =
@"Base = lambda *args, **kwargs: None
";
            var sExp =
@"Base=lambda *args,**kwargs: None
";
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, ParseStmt(pySrc).ToString());
        }
    }
}
