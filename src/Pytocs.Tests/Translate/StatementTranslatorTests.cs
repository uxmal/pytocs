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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Pytocs.UnitTests.Translate
{
    public class StatementTranslatorTests
    {
        public StatementTranslatorTests()
        {
            scope = new State(null, State.StateType.MODULE);
        }

        private static readonly string nl = Environment.NewLine;

        private readonly State scope;

        private string XlatStmts(string pyStmt)
        {
            StringReader rdr = new StringReader(pyStmt);
            Lexer lex = new Lexer("foo.py", rdr);
            CommentFilter flt = new CommentFilter(lex);
            Parser par = new Parser("foo.py", flt);
            List<Statement> stm = par.stmt();
            CodeGenerator gen = new CodeGenerator(new CodeCompileUnit(), "", "module");
            gen.SetCurrentMethod(new CodeMemberMethod());
            TypeReferenceTranslator types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            StatementTranslator xlt =
                new StatementTranslator(null, types, gen, new SymbolGenerator(), new HashSet<string>());
            stm[0].Accept(xlt);
            CSharpCodeProvider pvd = new CSharpCodeProvider();
            StringWriter writer = new StringWriter();
            foreach (CodeStatement csStmt in gen.Scope)
            {
                pvd.GenerateCodeFromStatement(
                    csStmt,
                    writer,
                    new CodeGeneratorOptions());
            }

            return writer.ToString();
        }

        private string XlatModule(string pyModule)
        {
            StringReader rdr = new StringReader(pyModule);
            Lexer lex = new Lexer("foo.py", rdr);
            CommentFilter flt = new CommentFilter(lex);
            Parser par = new Parser("foo.py", flt);
            List<Statement> stm = par.stmt();
            CodeCompileUnit unt = new CodeCompileUnit();
            CodeGenerator gen = new CodeGenerator(unt, "test", "testModule");
            TypeReferenceTranslator types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            StatementTranslator xlt =
                new StatementTranslator(null, types, gen, new SymbolGenerator(), new HashSet<string>());
            stm[0].Accept(xlt);
            CSharpCodeProvider pvd = new CSharpCodeProvider();
            StringWriter writer = new StringWriter();
            foreach (CodeNamespace ns in unt.Namespaces)
            {
                foreach (CodeNamespaceImport imp in ns.Imports)
                {
                    writer.WriteLine("using {0};", SanitizeNamespace(imp.Namespace, gen));
                }

                foreach (CodeTypeDeclaration type in ns.Types)
                {
                    pvd.GenerateCodeFromType(
                        type,
                        writer,
                        new CodeGeneratorOptions());
                }
            }

            return writer.ToString();
        }

        /// <summary>
        ///     Ensures no component of the namespace is a C# keyword.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private string SanitizeNamespace(string nmspace, CodeGenerator gen)
        {
            return string.Join(".",
                nmspace.Split('.')
                    .Select(n => gen.EscapeKeywordName(n)));
        }

        private string XlatMember(string pyModule)
        {
            StringReader rdr = new StringReader(pyModule);
            Lexer lex = new Lexer("foo.py", rdr);
            Parser par = new Parser("foo.py", lex);
            List<Statement> stm = par.stmt();
            CodeCompileUnit unt = new CodeCompileUnit();
            CodeGenerator gen = new CodeGenerator(unt, "test", "testModule");
            TypeReferenceTranslator types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            StatementTranslator xlt =
                new StatementTranslator(null, types, gen, new SymbolGenerator(), new HashSet<string>());
            stm[0].Accept(xlt);
            CSharpCodeProvider pvd = new CSharpCodeProvider();
            StringWriter writer = new StringWriter();
            foreach (CodeNamespace ns in unt.Namespaces)
            {
                foreach (CodeMember member in ns.Types[0].Members)
                {
                    pvd.GenerateCodeFromMember(
                        member, writer,
                        new CodeGeneratorOptions());
                    writer.WriteLine();
                }
            }

            return writer.ToString();
        }

        private void AssertEqual(string sExp, string sActual)
        {
            if (sExp != sActual)
            {
                Debug.WriteLine(sActual);
                Assert.Equal(sExp, sActual);
            }
        }

        private string FixCr(string sExp)
        {
            return sExp.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        [Fact]
        public void Class_ComputedSlots()
        {
            string pyCls =
                @"class MyClass():
    __slots__ = [x for x, _ in meta.fields]
";

            string sExp =
                @"public class MyClass {
}

";
            Assert.Equal(sExp, XlatMember(pyCls));
        }

        [Fact]
        public void Stmt_AddEq()
        {
            string pyStm = "s += 'foo'\n";
            string sExp = "s += \"foo\";\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_Async()
        {
            string pySrc =

            #region Expected

                @"async def frobAsync():
    await frobber
";
            string sExp =
                @"using System.Threading.Tasks;
public static class testModule {
    public async static Task<object> frobAsync() {
        await frobber;
    }
}
";

            #endregion Expected

            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Assign()
        {
            Assert.Equal("a = b;\r\n", XlatStmts("a = b\n"));
        }

        [Fact]
        public void Stmt_AssignTuple()
        {
            string pySrc =
                @"foo, bar = baz()
";
            string sExp =
                @"_tup_1 = baz();
foo = _tup_1.Item1;
bar = _tup_1.Item2;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_AssignTuple_Dummy()
        {
            string pySrc =
                @"_, bar = baz()
";
            string sExp =
                @"_tup_1 = baz();
bar = _tup_1.Item2;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_AssignTuple_Nonlocals()
        {
            string pySrc =
                @"foo.x, foo.y = baz()
";
            string sExp =
                @"_tup_1 = baz();
foo.x = _tup_1.Item1;
foo.y = _tup_1.Item2;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_ClassDocComment()
        {
            string pyStm = "class foo:\n    'doc comment'\n";
            string sExp =
                @"public static class testModule {
    // doc comment
    public class foo {
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void Stmt_ClassWithStaticMethods()
        {
            string src =
                @"class Foo:
    def method1():
        pass
    def method2():
        return 3
";
            string sExp =
                @"public static class testModule {
    public class Foo {
        public static object method1() {
        }

        public static object method2() {
            return 3;
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(src));
        }

        [Fact]
        public void Stmt_Comment_Before_Else_Clause()
        {
            string pySrc =
                @"if foo:
    foonicate()
" + @"# wasn't foo, try bar
else:
    barnicate()
";
            string sExp =
                @"if (foo) {
    foonicate();
} else {
    // wasn't foo, try bar
    barnicate();
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_CommentedArg()
        {
            string pyStm =
                @"foo(
        bar,
" + "        #baz" + nl +
                "     )" + nl;
            string sExp = "foo(bar);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_CtorWithDefaultArgs()
        {
            string pyStm = "class Class:\n    def __init__(self,a, b = 'q', c = 'cc'): pass\n";
            string sExp =
                @"public class Class {
    public Class(object a, object b = ""q"", object c = ""cc"") {
    }
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_Decoration()
        {
            string pyStm = "@dec.ora.tion\ndef foo():\n   pass\n";
            string sExp =
                @"[dec.ora.tion]
public static object foo() {
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_Decoration2()
        {
            string pySrc =
                @"@functools.wraps(f)
def wrapper(*args, **kwargs):
    pass
";
            string sExp =
                @"[functools.wraps(f)]
public static object wrapper(Hashtable kwargs, params object [] args) {
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_Def()
        {
            string sExp =
                @"public static class testModule {
    public static object a(object self, object bar) {
        Console.WriteLine(""Hello "" + bar);
    }
}
";
            Assert.Equal(sExp, XlatModule("def a(self,bar):\n print 'Hello ' + bar\n"));
        }

        [Fact]
        public void Stmt_DefWithDefaultArgs()
        {
            string pySrc =
                @"def foo(phoo, bar = 'hello', baz = 3):
    pass
";
            string sExp =
                @"public static object foo(object phoo, object bar = ""hello"", object baz = 3) {
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_DelFromDictionary()
        {
            string pySrc =
                @"def foo():
   del items[bar]
";
            string sExp =
                @"public static object foo() {
    items.Remove(bar);
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_DocComment_And_Comment()
        {
            string pySrc =
                @"def func():
    " + @"# fnord
    '''
    doc comment
    '''
    return 0
";
            string sExp =
                @"//
//     doc comment
//
public static object func() {
    // fnord
    return 0;
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_DocString()
        {
            string pySrc =
                @"def foo():
    '''
    Doc string
    '''
    pass
";
            string sExp =
                @"public static class testModule {
    //
    //     Doc string
    //
    public static object foo() {
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_EmptyCommentLine()
        {
            string pyStm = "# Comment\n";
            string sExp = "// Comment\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_EmptyLineWithSpaces()
        {
            string pyStm =
                @"def aterm(self, _t):
    ret = None

    aterm_AST_in = None

";
            string sExp =
                @"public static object aterm(object self, object _t) {
    object ret = null;
    object aterm_AST_in = null;
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_Exec()
        {
            string pyStm = "exec a in b, c\n";
            string sExp = "Python_Exec(a, b, c);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_For_Tuple_regression1()
        {
            string pySrc =
                @"for exit_stmt_id, target_addr in targets:
    if target_addr is None:
        addr_strs.append(""default"")
    else:
        addr_strs.append(""%#x"" % target_addr)
";
            string sExp =
                @"foreach (var _tup_1 in targets) {
    exit_stmt_id = _tup_1.Item1;
    target_addr = _tup_1.Item2;
    if (target_addr == null) {
        addr_strs.append(""default"");
    } else {
        addr_strs.append(String.Format(""%#x"", target_addr));
    }
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_foreach_class_property()
        {
            string pySrc =
                @"for self.target_addr in targets:
    if self.target_addr is None:
        addr_strs.append(""default"")
    else:
        addr_strs.append(""%#x"" % target_addr)
";
            string sExp =
                @"foreach (var _tmp_1 in targets) {
    this.target_addr = _tmp_1;
    if (this.target_addr == null) {
        addr_strs.append(""default"");
    } else {
        addr_strs.append(String.Format(""%#x"", target_addr));
    }
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_foreach_tuple()
        {
            string pySrc =
                @"for a,b in foo:
    print(a + b)
";
            string sExp =
                @"foreach (var _tup_1 in foo) {
    a = _tup_1.Item1;
    b = _tup_1.Item2;
    Console.WriteLine(a + b);
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_From()
        {
            string src =
                @"from foo.baz import niz as nilz
";
            string sExp =
                @"using nilz = foo.baz.niz;
public static class testModule {
}
";
            Assert.Equal(sExp, XlatModule(src));
        }

        // "Reported in GitHub issue 29
        [Fact]
        public void Stmt_funcdef_excess_positionalParameters()
        {
            string pySrc =
                @"def foo(*args):
    return len(args)
";
            string sExp =
                @"public static class testModule {
    public static object foo(params object [] args) {
        return args.Count;
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        // Reported in GitHub #15
        [Fact]
        public void Stmt_Global()
        {
            string pySrc =
                @"class foo:
    g = 42

    def fn():
        global g
        g = 0
";
            string sExp =

            #region Expected

                @"public static class testModule {
    public class foo {
        public object g = 42;

        public static object fn() {
            g = 0;
        }
    }
}
";

            #endregion Expected

            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_If()
        {
            Assert.Equal("if (a) {\r\n    b();\r\n}\r\n", XlatStmts("if a:\n  b()\n"));
        }

        [Fact]
        public void Stmt_IfWithCommentEmptyCommentLine()
        {
            string pyStm =
                @"if args:" + nl +
                "    # Comment" + nl +
                "    args = 0" + nl;
            string sExp =
                @"if (args) {
    // Comment
    args = 0;
}
";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_YieldExpr()
        {
            string pyStm = "yield 3\n";
            string sExp = "yield return 3;" + nl;
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_YieldNoExpr()
        {
            string pyStm = "yield\n";
            string sExp = "yield return null;" + nl;
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_InitCallOfBaseClass()
        {
            string pyStm = "class Foo(Bar):\n    def __init__(self): Bar.__init__(self, 'x');a = 3\n";
            string sExp =
                @"public class Foo
    : Bar {
    public Foo()
        : base(""x"") {
        var a = 3;
    }
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_InitMethodsToCtors()
        {
            string pyStm = "class foo:\n    def __init__(self,str): print(\"Hello \" + str)\n";
            string sExp =
                @"public class foo {
    public foo(object str) {
        Console.WriteLine(""Hello "" + str);
    }
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_issue_40()
        {
            string pySrc = "(a,b,c) = get_tuple()";
            string sExp = @"(a, b, c) = get_tuple();
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        // Reported in https://github.com/uxmal/pytocs/issues/56
        [Fact(DisplayName = nameof(Stmt_issue_65))]
        public void Stmt_issue_65()
        {
            string pySrc =
                @"def foo(replay_buffer, verbose=0, *, requires_vec_env, policy_base, policy_kwargs=None):
    pass
";
            string sExp =
                @"public static object foo(
    object replay_buffer,
    object verbose = 0,
    object requires_vec_env,
    object policy_base,
    object policy_kwargs = null) {
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_Lambda()
        {
            string pyStm =
                @"if func is None:
	self.func = lambda term: True
else:
	self.func = func
";
            string sExp =
                @"if (func == null) {
    this.func = term => true;
} else {
    this.func = func;
}
";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_Lambda_2Args()
        {
            string pyStm =
                @"if func is None:
	self.func = lambda a,b: True
else:
	self.func = func
";
            string sExp =
                @"if (func == null) {
    this.func = (a,b) => true;
} else {
    this.func = func;
}
";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_LambdaWithParams()
        {
            string pySrc =
                @"Base = lambda *args, **kwargs: None
";
            string sExp =
                @"Base = (args,kwargs) => null;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_LocalVar()
        {
            string pyStm = "def fn():\n    loc = 4\n    return loc\n";
            string sExp =
                @"public static object fn() {
    var loc = 4;
    return loc;
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_Long_ArgList()
        {
            string pySrc =
                @"def foo(arg1, arg2, arg3, arg4, arg5):
    return arg1[arg2] + arg3[arg4] + arg5
";
            string sExp =
                @"public static class testModule {
    public static object foo(
        object arg1,
        object arg2,
        object arg3,
        object arg4,
        object arg5) {
        return arg1[arg2] + arg3[arg4] + arg5;
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_MemberVarInit()
        {
            string src =
                @"
class Bar:
   member = 0
   def print_me(self):
      print member
";
            string sExp =
                @"public static class testModule {
    public class Bar {
        public object member = 0;

        public virtual object print_me() {
            Console.WriteLine(member);
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(src));
        }

        [Fact]
        public void Stmt_Method_Comments()
        {
            string pySrc =
                @"class Foo:
    " + @"# method comment
    def method(self):
        pass
";
            string sExp =
                @"public static class testModule {
    public class Foo {
        // method comment
        public virtual object method() {
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Multiple_Method_Comments()
        {
            string pySrc =
                @"class Foo:
    " + @"# method comment
    def method1(self):
        pass
    " + @"# another method
    def method2(self, arg1):
        return arg1 + 1
";
            string sExp =
                @"public static class testModule {
    public class Foo {
        // method comment
        public virtual object method1() {
        }

        // another method
        public virtual object method2(object arg1) {
            return arg1 + 1;
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_NestedDef()
        {
            string pySrc =
                @"def foo():
    bar = 4

    " + "#" + @" inner fn should become C# lambda
    def baz(a, b):
        print (""Bar squared"" + bar * bar)
        return False

    baz('3', 4)
";
            string sExp =
                @"public static object foo() {
    var bar = 4;
    // inner fn should become C# lambda
    Func<object, object, object> baz = (a,b) => {
        Console.WriteLine(""Bar squared"" + bar * bar);
        return false;
    };
    baz(""3"", 4);
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_ParameterWithNonConstantInitializer()
        {
            string pyStm = "def foo(a = bar.baz): return a\n";
            string sExp =
                @"public static object foo(object a = bar.baz) {
    return a;
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_Print()
        {
            string pyStm = "print >>sys.stderr, fmt % (line,col,text)\r\n";
            string sExp = "sys.stderr.WriteLine(String.Format(fmt, line, col, text));\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact(DisplayName = nameof(Stmt_print_trailing_comma))]
        public void Stmt_print_trailing_comma()
        {
            string pySrc =
                @"print('Hello:'),
";
            string sExp = "Console.Write(\"Hello:\");" + Environment.NewLine;

            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_PrintEmpty()
        {
            string pyStm = "print\n";
            string sExp = "Console.WriteLine();\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_Property()
        {
            string pySrc =
                @"class foo:

    @property
    def size():
        return 3
";
            string sExp =
                @"public static class testModule {
    public class foo {
        public object size {
            get {
                return 3;
            }
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Property_DocComment()
        {
            string pySrc =
                @"class foo:

    @property
    def size():
        ''' DocComment '''
        return x
";
            string sExp =
                @"public static class testModule {
    public class foo {
        //  DocComment
        public object size {
            get {
                return x;
            }
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Property_Method()
        {
            string pySrc =
                @"class foo:

    @property
    def size():
        return x

    def method():
        print ""Hello""
";
            string sExp =
                @"public static class testModule {
    public class foo {
        public object size {
            get {
                return x;
            }
        }

        public static object method() {
            Console.WriteLine(""Hello"");
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Property_Setter()
        {
            string pySrc =
                @"class foo:

    @property
    def size():
        ''' DocComment '''
        return self.x
    @size.setter
    def size(val):
        self.x = val
";
            string sExp =
                @"public static class testModule {
    public class foo {
        //  DocComment
        public object size {
            get {
                return this.x;
            }
            set {
                this.x = val;
            }
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Property_WithAssignment()
        {
            string pySrc =
                @"class foo:

    @property
    def size():
        a = 3
        return a
";
            string sExp =
                @"public static class testModule {
    public class foo {
        public object size {
            get {
                var a = 3;
                return a;
            }
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Regress1()
        {
            string pyStm =
                @"def __init__(self,**argv):
    Token.__init__(self,**argv)
";
            string sExp =
                @"using System.Collections;
public static class testModule {
    public static object @__init__(object self, Hashtable argv) {
        Token.@__init__(this, argv);
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void Stmt_Regress2()
        {
            string pyStm =
                @"def __init__(self,inst):
    if isinstance(inst,TokenStream):
        self.inst = inst
        return
    raise TypeError(""TokenStreamIterator requires TokenStream object"")
";
            string sExp =
                @"public static class testModule {
    public static object @__init__(object self, object inst) {
        if (inst is TokenStream) {
            this.inst = inst;
            return;
        }
        throw TypeError(""TokenStreamIterator requires TokenStream object"");
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void Stmt_Regress4()
        {
            string pySrc =
                @"class foo:
    def __repr__(self):
        return 'foo'

    " + @"#
    " + @"# Compatibility
    " + @"#

    @property
    @deprecated(replacement='simos')
    def _simos(self):
        return self.simos
";
            string sExp =

            #region Expected

                @"public static class testModule {
    public class foo {
        public virtual object @__repr__() {
            return ""foo"";
        }

        public object _simos {
            get {
                return this.simos;
            }
        }
    }
}
";

            #endregion Expected

            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Regression()
        {
            string pySrc = @"
def func(cfg_node):  # line-end comment
    """"""
    This is a doc comment
    """"""
    raise Dep()
";
            string sExp =
                @"//
//     This is a doc comment
//
public static object func(object cfg_node) {
    // line-end comment
    throw Dep();
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_ReturnExprList()
        {
            string pyStm = "return a, b\r\n";
            string sExp = "return Tuple.Create(a, b);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_TrailingComment()
        {
            string pyStm =
                "try:" + nl +
                "    bonehead()" + nl +
                "#if toast whine." + nl +
                "except:" + nl +
                "    whine()" + nl;
            string sExp =
                @"try {
    bonehead();
} catch {
    //if toast whine.
    whine();
}
";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact(DisplayName = nameof(Stmt_try_except_with))]
        public void Stmt_try_except_with()
        {
            string pySrc =
                @"def foo():
    try:
        with open(args.wasm_file, 'rb') as raw:
            raw = raw.read()
    except IOError as exc:
        print(""[-] Can't open input file: "" + str(exc), file=sys.stderr)
";
            string sExp =
                @"public static class testModule {
    public static object foo() {
        try {
            using (var raw = open(args.wasm_file, ""rb"")) {
                raw = raw.read();
            }
        } catch (IOError) {
            Console.WriteLine(""[-] Can't open input file: "" + exc.ToString(), file: sys.stderr);
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Tuple_Assign()
        {
            string pySrc =
                @"a,b = c,d
";
            string sExp =
                @"a = c;
b = d;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_Tuple_Assign_field()
        {
            string pySrc =
                @"a.b, c.de = 'e','f'
";
            string sExp =
                @"a.b = ""e"";
c.de = ""f"";
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_tuple_assignment_starred_target()
        {
            string pySrc = "a, b, *c = get_items()";
            string sExp =
                @"var _it_1 = get_items();
a = _it_1.Element(0);
b = _it_1.Element(1);
c = _it_1.Skip(2).ToList();
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_TupleArguments))]
        public void Stmt_TupleArguments()
        {
            string pySrc =
                @"class bar:
    def foo(self, (value, sort)):
       self.value = value
";

            string sExp =
                @"public static class testModule {
    public class bar {
        public virtual object foo(Tuple<object, object> _tup_1) {
            object sort = _tup_1.Item2;
            object value = _tup_1.Item1;
            this.value = value;
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_TupleSingletonAssignment()
        {
            string pySrc =
                @"yx, = sdf()
";
            string sExp =
                @"_tup_1 = sdf();
yx = _tup_1.Item1;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_Use_CSharpVar_Unless_Null()
        {
            string pySrc =
                @"def foo():
  x = 1
  node = None
  y = bar(x)
";
            string sExp =
                @"public static class testModule {
    public static object foo() {
        var x = 1;
        object node = null;
        var y = bar(x);
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_With()
        {
            string pySrc =
                @"with foo():
    bar()
";
            string sExp =
                @"using (var foo()) {
    bar();
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void StmtElif()
        {
            string pyStm =
                @"if x:
    foo()
    bar()
elif y:
    bar()
    foo()
else:
    qqq()
    foo()
";
            string sExp =
                @"if (x) {
    foo();
    bar();
} else if (y) {
    bar();
    foo();
} else {
    qqq();
    foo();
}
";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void StmtEmptyList()
        {
            string src = "res = []\r\n";
            string sExp = "res = new List<object>();\r\n";
            Assert.Equal(sExp, XlatStmts(src));
        }

        [Fact]
        public void StmtFnDefaultParameterValue()
        {
            string pyStm =
                @"def __init__(self, func = None):
    printf(func)
";
            string sExp =
                @"public static class testModule {
    public static object @__init__(object self, object func = null) {
        printf(func);
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void StmtForeach()
        {
            string src =
                @"for x in list:
    print x
";
            string sExp =
                @"foreach (var x in list) {
    Console.WriteLine(x);
}
";
            Assert.Equal(sExp, XlatStmts(src));
        }

        [Fact]
        public void StmtImport()
        {
            string src =
                @"import foo.bar
";
            string sExp =
                @"using foo.bar;
public static class testModule {
}
";
            Assert.Equal(sExp, XlatModule(src));
        }

        [Fact]
        public void StmtImport_ReservedWord()
        {
            string src =
                @"import struct.Foo
";
            string sExp =
                @"using @struct.Foo;
public static class testModule {
}
";
            Assert.Equal(sExp, XlatModule(src));
        }

        [Fact]
        public void StmtList()
        {
            string pyStm = "return (isinstance(x,str) or isinstance(x,unicode))\r\n";
            string sExp = "return x is str || x is unicode;\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void StmtListArgument()
        {
            string pyStm = "foo(*args)\r\n";
            string sExp = "foo(args);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void StmtRaise()
        {
            string src =
                "raise FooExc(\"Bob\", 1)\r\n";
            string sExp =
                "throw FooExc(\"Bob\", 1);\r\n";
            Assert.Equal(sExp, XlatStmts(src));
        }

        [Fact]
        public void StmtSetBuilder()
        {
            string pyStm =
                @"r = {
    'major'  : '2',
    'minor'  : '7',
}
";
            string sExp =
                @"r = new Dictionary<object, object> {
    {
        ""major"",
        ""2""},
    {
        ""minor"",
        ""7""}};
";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void StmtTry()
        {
            string src =
                @"try:
    otherelm = otherit.next()
except StopIteration:
    return ANCESTOR
finally:
    printf('Hello')
";
            string sExp =
                @"try {
    otherelm = otherit.next();
} catch (StopIteration) {
    return ANCESTOR;
} finally {
    printf(""Hello"");
}
";
            Assert.Equal(FixCr(sExp), XlatStmts(src));
        }

        [Fact]
        public void StmtVarargs()
        {
            string pyStm =
                @"def error(fmt,*args):
    return fmt
";
            string sExp =
                @"public static class testModule {
    public static object error(object fmt, params object [] args) {
        return fmt;
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void StmtWhile()
        {
            string src =
                @"while x:
    printf('Hello')
    x = x - 1
";
            string sExp =
                @"while (x) {
    printf(""Hello"");
    x = x - 1;
}
";
            Assert.Equal(sExp, XlatStmts(src));
        }

        [Fact]
        public void StmtWhileElse()
        {
            string src =
                @"while x:
    printf('Hello')
    x = x - 1
else:
    printf('Never')
";
            string sExp =
                @"if (x) {
    do {
        printf(""Hello"");
        x = x - 1;
    } while (x);
} else {
    printf(""Never"");
}
";
            Assert.Equal(sExp, XlatStmts(src));
        }
    }
}