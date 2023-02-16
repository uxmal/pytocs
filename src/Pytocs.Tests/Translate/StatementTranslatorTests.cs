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

using Pytocs.Core.CodeModel;
using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Syntax;
using Pytocs.Core.Types;
using Pytocs.Core.Translate;

namespace Pytocs.UnitTests.Translate
{
    public class StatementTranslatorTests
    {
        private static readonly string nl = Environment.NewLine;

        private readonly TypeReferenceTranslator datatypes;

        public StatementTranslatorTests()
        {
            this.datatypes = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
        }

        private FunType FnVoid(params DataType [] args)
        {
            return new FunType(new TupleType(args), DataType.None);
        }

        private string XlatStmts(string pyStmt)
        {
            var rdr = new StringReader(pyStmt);
            var lex = new Lexer("foo.py", rdr);
            var flt = new CommentFilter(lex);
            var par = new Parser("foo.py", flt);
            var stm = par.stmt();
            var gen = new CodeGenerator(new CodeCompileUnit(), "", "module");
            gen.SetCurrentFunction(new CodeMemberMethod());
            var xlt = new StatementTranslator(null, datatypes, gen, new SymbolGenerator(), new HashSet<string>());
            stm[0].Accept(xlt);
            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
            foreach (CodeStatement csStmt in gen.Scope)
            {
                pvd.GenerateCodeFromStatement(
                    csStmt,
                    writer,
                    new CodeGeneratorOptions
                    {
                    });
            }
            return writer.ToString();
        }

        private string XlatModule(string pyModule)
        {
            var rdr = new StringReader(pyModule);
            var lex = new Lexer("foo.py", rdr);
            var flt = new CommentFilter(lex);
            var par = new Parser("foo.py", flt);
            var stm = par.stmt();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new StatementTranslator(null, types, gen, new SymbolGenerator(), new HashSet<string>());
            stm[0].Accept(xlt);
            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
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
                    new CodeGeneratorOptions
                    {
                    });
                }
            }
            return writer.ToString();
        }

        /// <summary>
        /// Ensures no component of the namespace is a C# keyword.
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
            var rdr = new StringReader(pyModule);
            var lex = new Lexer("foo.py", rdr);
            var par = new Parser("foo.py", lex);
            var stm = par.stmt();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new StatementTranslator(null, types, gen, new SymbolGenerator(), new HashSet<string>());
            stm[0].Accept(xlt);
            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
            foreach (CodeNamespace ns in unt.Namespaces)
            {
                foreach (var member in ns.Types[0].Members)
                {
                    pvd.GenerateCodeFromMember(
                        member, writer,
                        new CodeGeneratorOptions
                        {
                        });
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

        [Fact]
        public void Stmt_Assign()
        {
            Assert.Equal("a = b;\r\n", XlatStmts("a = b\n"));
        }

        [Fact]
        public void Stmt_If()
        {
            Assert.Equal("if (a) {\r\n    b();\r\n}\r\n", XlatStmts("if a:\n  b()\n"));
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
        public void StmtForeach()
        {
            string src =
@"for x in list:
    print x
";
            var sExp =
@"foreach (var x in list) {
    Console.WriteLine(x);
}
";
            Assert.Equal(sExp, XlatStmts(src));
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
        public void StmtEmptyList()
        {
            string src = "res = []\r\n";
            string sExp = "res = new List<object>();\r\n";
            Assert.Equal(sExp, XlatStmts(src));
        }

        private string FixCr(string sExp)
        {
            return sExp.Replace("\r\n", "\n").Replace("\n", "\r\n");
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


        [Fact]
        public void Stmt_Lambda()
        {
            var pyStm =
@"if func is None:
	self.func = lambda term: True
else:
	self.func = func
";
            var sExp =
@"if (func is null) {
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
            var pyStm =
@"if func is None:
	self.func = lambda a,b: True
else:
	self.func = func
";
            var sExp =
@"if (func is null) {
    this.func = (a,b) => true;
} else {
    this.func = func;
}
";
            Assert.Equal(sExp, XlatStmts(pyStm));

        }

        [Fact]
        public void StmtFnDefaultParameterValue()
        {
            var pyStm =
@"def __init__(self, func = None):
    printf(func)
";
            var sExp =
@"public static class testModule {
    
    public static object @__init__(object self, object func = null) {
        printf(func);
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void StmtSetBuilder()
        {
            var pyStm =
@"r = {
    'major'  : '2',
    'minor'  : '7',   
}
";
            var sExp =
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
        public void StmtVarargs()
        {
            var pyStm =
@"def error(fmt,*args):
    return fmt
";
            var sExp =
@"public static class testModule {
    
    public static object error(object fmt, params object [] args) {
        return fmt;
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void StmtList()
        {
            var pyStm = "return (isinstance(x,str) or isinstance(x,unicode))\r\n";
            var sExp = "return x is str || x is unicode;\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void StmtListArgument()
        {
            var pyStm = "foo(*args)\r\n";
            var sExp = "foo(args);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void StmtElif()
        {
            var pyStm =
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
            var sExp =
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
        public void Stmt_Regress1()
        {
            var pyStm =
@"def __init__(self,**argv):
    Token.__init__(self,**argv)
";
            var sExp =
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
            var pyStm =
@"def __init__(self,inst):
    if isinstance(inst,TokenStream):
        self.inst = inst
        return
    raise TypeError(""TokenStreamIterator requires TokenStream object"")
";
            var sExp =
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
        public void Stmt_Print()
        {
            var pyStm = "print >>sys.stderr, fmt % (line,col,text)\r\n";
            var sExp = "sys.stderr.WriteLine(String.Format(fmt, line, col, text));\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }


        [Fact]
        public void Stmt_PrintEmpty()
        {
            var pyStm = "print\n";
            var sExp = "Console.WriteLine();\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_EmptyLineWithSpaces()
        {
            var pyStm =
@"def aterm(self, _t):    
    ret = None
    
    aterm_AST_in = None

";
            var sExp =
@"public static object aterm(object self, object _t) {
    object ret = null;
    object aterm_AST_in = null;
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_ReturnExprList()
        {
            var pyStm = "return a, b\r\n";
            var sExp = "return (a, b);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_Decoration()
        {
            var pyStm = "@dec.ora.tion\ndef foo():\n   pass\n";
            var sExp =
@"[dec.ora.tion]
public static object foo() {
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact(Skip = "This is a Python 2-ism")]
        public void Stmt_Exec()
        {
            var pyStm = "exec a in b, c\n";
            var sExp = "Python_Exec(a, b, c);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_InitMethodsToCtors()
        {
            var pyStm = "class foo:\n    def __init__(self,str): print(\"Hello \" + str)\n";
            var sExp =
@"public class foo {
    
    public foo(object str) {
        Console.WriteLine(""Hello "" + str);
    }
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_InitCallOfBaseClass()
        {
            var pyStm = "class Foo(Bar):\n    def __init__(self): Bar.__init__(self, 'x');a = 3\n";
            var sExp =
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
        public void Stmt_ParameterWithNonConstantInitializer()
        {
            var pyStm = "def foo(a = bar.baz): return a\n";
            var sExp =
@"public static object foo(object a = bar.baz) {
    return a;
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_CtorWithDefaultArgs()
        {
            var pyStm = "class Class:\n    def __init__(self,a, b = 'q', c = 'cc'): pass\n";
            var sExp =
@"public class Class {
    
    public Class(object a, object b = ""q"", object c = ""cc"") {
    }
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Class_ComputedSlots()
        {
            var pyCls =
@"class MyClass():
    __slots__ = [x for x, _ in meta.fields]
";

            var sExp =
@"public class MyClass {
}

";
            Assert.Equal(sExp, XlatMember(pyCls));
        }

        [Fact]
        public void Stmt_EmptyCommentLine()
        {
            var pyStm = "# Comment\n";
            var sExp = "// Comment\r\n";
            Assert.Equal(sExp, this.XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_IfWithCommentEmptyCommentLine()
        {
            var pyStm =
@"if args:" + nl +
"    # Comment" + nl +
"    args = 0" + nl;
            var sExp =
@"if (args) {
    // Comment
    args = 0;
}
";
            Assert.Equal(sExp, this.XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_CommentedArg()
        {
            var pyStm =
@"foo(
        bar,
" + "        #baz" + nl +
"     )" + nl;
            var sExp = "foo(bar);\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_TrailingComment()
        {
            var pyStm =
"try:" + nl +
"    bonehead()" + nl +
"#if toast whine." + nl +
"except:" + nl +
"    whine()" + nl;
            var sExp =
@"try {
    bonehead();
} catch {
    //if toast whine.
    whine();
}
";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_AddEq()
        {
            var pyStm = "s += 'foo'\n";
            var sExp = "s += \"foo\";\r\n";
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_ClassDocComment()
        {
            var pyStm = "class foo:\n    'doc comment'\n";
            var sExp =
@"public static class testModule {
    
    // doc comment
    public class foo {
    }
}
";
            Assert.Equal(sExp, XlatModule(pyStm));
        }

        [Fact]
        public void Stmt_LocalVar()
        {
            var pyStm = "def fn():\n    loc = 4\n    return loc\n";
            var sExp =
@"public static object fn() {
    var loc = 4;
    return loc;
}

";
            Assert.Equal(sExp, XlatMember(pyStm));
        }

        [Fact]
        public void Stmt_YieldExpr()
        {
            var pyStm = "yield 3\n";
            var sExp = "yield return 3;" + nl;
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_YieldNoExpr()
        {
            var pyStm = "yield\n";
            var sExp = "yield return null;" + nl;
            Assert.Equal(sExp, XlatStmts(pyStm));
        }

        [Fact]
        public void Stmt_Decoration2()
        {
            var pySrc =
@"@functools.wraps(f)
def wrapper(*args, **kwargs):
    pass
";
            var sExp =
@"[functools.wraps(f)]
public static object wrapper(Hashtable kwargs, params object [] args) {
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact]
        public void Stmt_With()
        {
            var pySrc =
@"with foo():
    bar()
";
            var sExp =
@"using (var foo()) {
    bar();
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_AssignTuple))]
        public void Stmt_AssignTuple()
        {
            var pySrc =
@"foo, bar = baz()
";
            var sExp =
@"(foo, bar) = baz();
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }


        [Fact(DisplayName = nameof(Stmt_AssignTuple_Dummy))]
        public void Stmt_AssignTuple_Dummy()
        {
            var pySrc =
@"_, bar = baz()
";
            var sExp =
@"(_, bar) = baz();
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_AssignTuple_Nonlocals))]
        public void Stmt_AssignTuple_Nonlocals()
        {
            var pySrc =
@"foo.x, foo.y = baz()
";
            var sExp =
@"(foo.x, foo.y) = baz();
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_TupleArguments))]
        public void Stmt_TupleArguments()
        {
            var pySrc =
@"class bar:
    def foo(self, (value, sort)):
       self.value = value
";

            var sExp =
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

        [Fact(DisplayName = nameof(Stmt_TupleSingletonAssignment))]
        public void Stmt_TupleSingletonAssignment()
        {
            var pySrc =
@"yx, = sdf()
";
            var sExp =
@"_tup_1 = sdf();
yx = _tup_1.Item1;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_LambdaWithParams()
        {
            var pySrc =
@"Base = lambda *args, **kwargs: None
";
            var sExp =
@"Base = (args,kwargs) => null;
";
            Assert.Equal(sExp, XlatStmts(pySrc).ToString());
        }

        [Fact]
        public void Stmt_DefWithDefaultArgs()
        {
            var pySrc =
@"def foo(phoo, bar = 'hello', baz = 3):
    pass
";
            var sExp =
@"public static object foo(object phoo, object bar = ""hello"", object baz = 3) {
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }

        [Fact]
        public void Stmt_DelFromDictionary()
        {
            var pySrc =
@"def foo():
   del items[bar]
";
            var sExp =
@"public static object foo() {
    items.Remove(bar);
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }

        [Fact(DisplayName = nameof(Stmt_NestedDef))]
        public void Stmt_NestedDef()
        {
            var pySrc =
@"def foo():
    bar = 4

    " + "#" + @" inner fn should become C# local function
    def baz(a, b):
        print (""Bar squared"" + bar * bar)
        return False

    baz('3', 4)
";
            var sExp =
@"public static object foo() {
    var bar = 4;
    // inner fn should become C# local function
    object baz(object a, object b) {
        Console.WriteLine(""Bar squared"" + bar * bar);
        return false;
    }
    baz(""3"", 4);
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }

        [Fact]
        public void Stmt_foreach_tuple()
        {
            var pySrc =
@"for a,b in foo:
    print(a + b)
";
            var sExp =
@"foreach (var (a, b) in foo) {
    Console.WriteLine(a + b);
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_foreach_class_property()
        {
            var pySrc =
@"for self.target_addr in targets:
    if self.target_addr is None:
        addr_strs.append(""default"")
    else:
        addr_strs.append(""%#x"" % target_addr)
";
            var sExp =
@"foreach (var _tmp_1 in targets) {
    this.target_addr = _tmp_1;
    if (this.target_addr is null) {
        addr_strs.append(""default"");
    } else {
        addr_strs.append(String.Format(""%#x"", target_addr));
    }
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_Tuple_Assign()
        {
            var pySrc =
@"a,b = c,d
";
            var sExp =
@"a = c;
b = d;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_Tuple_Assign_field()
        {
            var pySrc =
@"a.b, c.de = 'e','f'
";
            var sExp =
@"a.b = ""e"";
c.de = ""f"";
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_For_Tuple_regression1))]
        public void Stmt_For_Tuple_regression1()
        {
            var pySrc =
@"for exit_stmt_id, target_addr in targets:
    if target_addr is None:
        addr_strs.append(""default"")
    else:
        addr_strs.append(""%#x"" % target_addr)
";
            var sExp =
@"foreach (var (exit_stmt_id, target_addr) in targets) {
    if (target_addr is null) {
        addr_strs.append(""default"");
    } else {
        addr_strs.append(String.Format(""%#x"", target_addr));
    }
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_Use_CSharpVar_Unless_Null()
        {
            var pySrc =
@"def foo():
  x = 1
  node = None
  y = bar(x)
";
            var sExp =
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
        public void Stmt_DocString()
        {
            var pySrc =
@"def foo():
    '''
    Doc string
    '''
    pass
";
            var sExp =
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

        [Fact(DisplayName = nameof(Stmt_try_except_with))]
        public void Stmt_try_except_with()
        {
            var pySrc =
@"def foo():
    try:
        with open(args.wasm_file, 'rb') as raw:
            raw = raw.read()
    except IOError as exc:
        print(""[-] Can't open input file: "" + str(exc), file=sys.stderr)
";
            var sExp = 
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
        public void Stmt_Property()
        {
            var pySrc =
@"class foo:

    @property
    def size():
        return 3
";
            var sExp =
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
        public void Stmt_Property_WithAssignment()
        {
            var pySrc =
@"class foo:

    @property
    def size():
        a = 3
        return a
";
            var sExp =
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
        public void Stmt_Property_Method()
        {
            var pySrc =
@"class foo:

    @property
    def size():
        return x

    def method():
        print ""Hello""
";
            var sExp =
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
        public void Stmt_Property_DocComment()
        {
            var pySrc =
@"class foo:

    @property
    def size():
        ''' DocComment '''
        return x
";
            var sExp =
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
        public void Stmt_Property_Setter()
        {
            var pySrc =
@"class foo:

    @property
    def size():
        ''' DocComment '''
        return self.x
    @size.setter
    def size(val):
        self.x = val
";
            var sExp =
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
        public void Stmt_Long_ArgList()
        {
            var pySrc =
@"def foo(arg1, arg2, arg3, arg4, arg5):
    return arg1[arg2] + arg3[arg4] + arg5
";
            var sExp =
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
        public void Stmt_Method_Comments()
        {
            var pySrc =
@"class Foo:
    " + @"# method comment
    def method(self):
        pass
";
            var sExp =
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
            var pySrc =
@"class Foo:
    " + @"# method comment
    def method1(self):
        pass
    " + @"# another method
    def method2(self, arg1):
        return arg1 + 1
";
            var sExp =
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
        public void Stmt_Comment_Before_Else_Clause()
        {
            var pySrc =
@"if foo:
    foonicate()
" + @"# wasn't foo, try bar
else:
    barnicate()
";
            var sExp =
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
        public void Stmt_DocComment_And_Comment()
        {
            var pySrc =
@"def func():
    " + @"# fnord
    '''
    doc comment
    '''
    return 0
";
            var sExp =
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
        public void Stmt_Regression()
        {
            var pySrc = @"
def func(cfg_node):  # line-end comment
    """"""
    This is a doc comment
    """"""
    raise Dep()
";
            var sExp =
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
        public void Stmt_Regress4()
        {
            var pySrc =
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
            var sExp =
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
            #endregion
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        // Reported in GitHub #15
        [Fact]
        public void Stmt_Global()
        {
            var pySrc =
@"class foo:
    g = 42

    def fn():
        global g
        g = 0
";
            var sExp =
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
            #endregion
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_Async()
        {
            var pySrc =
            #region Expected
@"async def frobAsync():
    await frobber
";
            var sExp =
@"using System.Threading.Tasks;
public static class testModule {
    
    public async static Task<object> frobAsync() {
        await frobber;
    }
}
";
            #endregion
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        // "Reported in GitHub issue 29
        [Fact]
        public void Stmt_funcdef_excess_positionalParameters()
        {
            var pySrc =
@"def foo(*args):
    return len(args)
";
            var sExp =
@"public static class testModule {
    
    public static object foo(params object [] args) {
        return args.Count;
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_print_trailing_comma))]
        public void Stmt_print_trailing_comma()
        {
            var pySrc =
@"print('Hello:'),
";
            var sExp = "Console.Write(\"Hello:\");" + Environment.NewLine;

            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        // Reported in https://github.com/uxmal/pytocs/issues/56
        [Fact(DisplayName = nameof(Stmt_issue_65))]
        public void Stmt_issue_65()
        {
            var pySrc =
@"def foo(replay_buffer, verbose=0, *, requires_vec_env, policy_base, policy_kwargs=None):
    pass
";
            var sExp =
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
        public void Stmt_issue_40()
        {
            var pySrc = "(a,b,c) = get_tuple()";
            var sExp = @"(a, b, c) = get_tuple();
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_tuple_assignment_starred_target()
        {
            var pySrc = "a, b, *c = get_items()";
            var sExp =
@"var _it_1 = get_items();
a = _it_1.Element(0);
b = _it_1.Element(1);
c = _it_1.Skip(2).ToList();
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact]
        public void Stmt_def_ellipsis()
        {
            var pySrc = "def func(arg):\n    ...\n";
            var sExp =
@"public static object func(object arg) {
}

";
            Assert.Equal(sExp, XlatMember(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_ChainedComparison))]
        public void Stmt_ChainedComparison()
        {
            var pySrc = "valid = 0 <= value < maxValue";
            var sExp = @"valid = 0 <= value && value < maxValue;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_ChainedComparison_SideEffect))]
        public void Stmt_ChainedComparison_SideEffect()
        {
            var pySrc = "valid = 0 <= sideEffect() < maxValue";
            var sExp =
@"_tmp_1 = sideEffect();
valid = 0 <= _tmp_1 && _tmp_1 < maxValue;
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_TryElse))]
        public void Stmt_TryElse()
        {
            var pySrc =
@"try:
    stmt1()
except (Ex1):
    exception1()
else:
    succeeded()
finally:
    finalStmt()
";
            var sExp =
@"_success1 = false;
try {
    stmt1();
    _success1 = true;
} catch (Ex1) {
    exception1();
} finally {
    finalStmt();
}
if (_success1) {
    succeeded();
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }


        [Fact(DisplayName = nameof(Stmt_AssignmentExpression))]
        public void Stmt_AssignmentExpression()
        {
            var pySrc = @"
while chunk := read(256):
    process(chunk)
";
            var sExp =
@"while ((chunk = read(256)) != null) {
    process(chunk);
}
";
            Assert.Equal(sExp, XlatStmts(pySrc));
        }

        [Fact(DisplayName = nameof(Stmt_Assign_Assign))]
        public void Stmt_Assign_Assign()
        {
            var pySrc = @"
x = y = fun(z)
";
            var sExpected =
@"x = y = fun(z);
";
            Assert.Equal(sExpected, XlatStmts(pySrc));
        }


        [Fact(DisplayName = nameof(Stmt_AugmentedMatrixMultiplication))]
        public void Stmt_AugmentedMatrixMultiplication()
        {
            var pySrc = "a @= b";
            var sExpected =
@"a = a.@__imatmul__(b);
";
            Assert.Equal(sExpected, XlatStmts(pySrc));
        }
    }
}
