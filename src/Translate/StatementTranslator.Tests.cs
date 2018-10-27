#region License
//  Copyright 2015-2018 John Källén
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

#if DEBUG
using Pytocs.CodeModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.TypeInference;
using Pytocs.Syntax;
using Pytocs.Types;

namespace Pytocs.Translate
{
    [TestFixture]
    public class StatementTranslatorTests
    {
        private static readonly string nl = Environment.NewLine;

        private State scope;

        public void Setup()
        {
            scope = new State(null, State.StateType.MODULE);
        }

        private string XlatStmts(string pyStmt)
        {
            var rdr = new StringReader(pyStmt);
            var lex = new Syntax.Lexer("foo.py", rdr);
            var flt = new Syntax.CommentFilter(lex);
            var par = new Syntax.Parser("foo.py", flt);
            var stm = par.stmt();
            var gen = new CodeGenerator(new CodeCompileUnit(), "", "module");
            gen.SetCurrentMethod(new CodeMemberMethod());
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new StatementTranslator(types, gen, new SymbolGenerator(), new HashSet<string>());
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
            var lex = new Syntax.Lexer("foo.py", rdr);
            var flt = new Syntax.CommentFilter(lex);
            var par = new Syntax.Parser("foo.py", flt);
            var stm = par.stmt();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new StatementTranslator(types, gen, new SymbolGenerator(), new HashSet<string>());
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
            var lex = new Syntax.Lexer("foo.py", rdr);
            var par = new Syntax.Parser("foo.py", lex);
            var stm = par.stmt();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new StatementTranslator(types, gen, new SymbolGenerator(), new HashSet<string>());
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

        [Test]
        public void Stmt_Assign()
        {
            Assert.AreEqual("a = b;\r\n", XlatStmts("a = b\n"));
        }

        [Test]
        public void Stmt_If()
        {
            Assert.AreEqual("if (a) {\r\n    b();\r\n}\r\n", XlatStmts("if a:\n  b()\n"));
        }

        [Test]
        public void Stmt_Def()
        {
            string sExp =
@"public static class testModule {
    
    public static object a(object self, object bar) {
        Console.WriteLine(""Hello "" + bar);
    }
}
";
            Assert.AreEqual(sExp, XlatModule("def a(self,bar):\n print 'Hello ' + bar\n"));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(src));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(src));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(src));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(src));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(src));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(src));
        }

        [Test]
        public void StmtRaise()
        {
            string src =
                "raise FooExc(\"Bob\", 1)\r\n";
            string sExp =
                "throw FooExc(\"Bob\", 1);\r\n";
            Assert.AreEqual(sExp, XlatStmts(src));
        }

        [Test]
        public void StmtEmptyList()
        {
            string src = "res = []\r\n";
            string sExp = "res = new List<object>();\r\n";
            Assert.AreEqual(sExp, XlatStmts(src));
        }

        private string FixCr(string sExp)
        {
            return sExp.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        [Test]
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
            Assert.AreEqual(FixCr(sExp), XlatStmts(src));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(src));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(src));
        }


        [Test]
        public void Stmt_Lambda()
        {
            var pyStm =
@"if func is None:
	self.func = lambda term: True
else:
	self.func = func
";
            var sExp =
@"if (func == null) {
    this.func = term => true;
} else {
    this.func = func;
}
";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
        public void Stmt_Lambda_2Args()
        {
            var pyStm =
@"if func is None:
	self.func = lambda a,b: True
else:
	self.func = func
";
            var sExp =
@"if (func == null) {
    this.func = (a,b) => true;
} else {
    this.func = func;
}
";
            Assert.AreEqual(sExp, XlatStmts(pyStm));

        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyStm));
        }

        [Test]
        public void StmtList()
        {
            var pyStm = "return (isinstance(x,str) or isinstance(x,unicode))\r\n";
            var sExp = "return x is str || x is unicode;\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
        public void StmtListArgument()
        {
            var pyStm = "foo(*args)\r\n";
            var sExp = "foo(args);\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyStm));
        }

        [Test]
        public void Stmt_Print()
        {
            var pyStm = "print >>sys.stderr, fmt % (line,col,text)\r\n";
            var sExp = "sys.stderr.WriteLine(String.Format(fmt, line, col, text));\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }


        [Test]
        public void Stmt_PrintEmpty()
        {
            var pyStm = "print\n";
            var sExp = "Console.WriteLine();\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pyStm));
        }

        [Test]
        public void Stmt_ReturnExprList()
        {
            var pyStm = "return a, b\r\n";
            var sExp = "return Tuple.Create(a, b);\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
        public void Stmt_Decoration()
        {
            var pyStm = "@dec.ora.tion\ndef foo():\n   pass\n";
            var sExp =
@"[dec.ora.tion]
public static object foo() {
}

";
            Assert.AreEqual(sExp, XlatMember(pyStm));
        }

        [Test]
        public void Stmt_Exec()
        {
            var pyStm = "exec a in b, c\n";
            var sExp = "Python_Exec(a, b, c);\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pyStm));
        }

        [Test]
        public void Stmt_ParameterWithNonConstantInitializer()
        {
            var pyStm = "def foo(a = bar.baz): return a\n";
            var sExp =
@"public static object foo(object a = bar.baz) {
    return a;
}

";
            Assert.AreEqual(sExp, XlatMember(pyStm));
        }

        [Test]
        public void Stmt_CtorWithDefaultArgs()
        {
            var pyStm = "class Class:\n    def __init__(self,a, b = 'q', c = 'cc'): pass\n";
            var sExp =
@"public class Class {
    
    public Class(object a, object b = ""q"", object c = ""cc"") {
    }
}

";
            Assert.AreEqual(sExp, XlatMember(pyStm));
        }

        [Test]
        public void Class_slots()
        {
            var pyCls =
@"class MyClass:
   __slots__ = [ 'foo', 'bar', 'baz' ]
";
            var sExp =
@"public class MyClass {
    
    public object foo;
    
    public object bar;
    
    public object baz;
}

";
            Assert.AreEqual(sExp, XlatMember(pyCls));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pyCls));
        }

        [Test]
        public void Stmt_EmptyCommentLine()
        {
            var pyStm = "# Comment\n";
            var sExp = "// Comment\r\n";
            Assert.AreEqual(sExp, this.XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, this.XlatStmts(pyStm));
        }

        [Test]
        public void Stmt_CommentedArg()
        {
            var pyStm =
@"foo(
        bar,
" + "        #baz" + nl +
"     )" + nl;
            var sExp = "foo(bar);\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
        public void Stmt_AddEq()
        {
            var pyStm = "s += 'foo'\n";
            var sExp = "s += \"foo\";\r\n";
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyStm));
        }

        [Test]
        public void Stmt_LocalVar()
        {
            var pyStm = "def fn():\n    loc = 4\n    return loc\n";
            var sExp =
@"public static object fn() {
    var loc = 4;
    return loc;
}

";
            Assert.AreEqual(sExp, XlatMember(pyStm));
        }

        [Test]
        public void Stmt_YieldExpr()
        {
            var pyStm = "yield 3\n";
            var sExp = "yield return 3;" + nl;
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
        public void Stmt_YieldNoExpr()
        {
            var pyStm = "yield\n";
            var sExp = "yield return null;" + nl;
            Assert.AreEqual(sExp, XlatStmts(pyStm));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
        public void Stmt_AssignTuple()
        {
            var pySrc =
@"foo, bar = baz()
";
            var sExp =
@"_tup_1 = baz();
foo = _tup_1.Item1;
bar = _tup_1.Item2;
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }


        [Test]
        public void Stmt_AssignTuple_Dummy()
        {
            var pySrc =
@"_, bar = baz()
";
            var sExp =
@"_tup_1 = baz();
bar = _tup_1.Item2;
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
        public void Stmt_AssignTuple_Nonlocals()
        {
            var pySrc =
@"foo.x, foo.y = baz()
";
            var sExp =
@"_tup_1 = baz();
foo.x = _tup_1.Item1;
foo.y = _tup_1.Item2;
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
        public void Stmt_TupleSingletonAssignment()
        {
            var pySrc =
@"yx, = sdf()
";
            var sExp =
@"_tup_1 = sdf();
yx = _tup_1.Item1;
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
        public void Stmt_LambdaWithParams()
        {
            var pySrc =
@"Base = lambda *args, **kwargs: None
";
            var sExp =
@"Base = (args,kwargs) => null;
";
            Assert.AreEqual(sExp, XlatStmts(pySrc).ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pySrc).ToString());
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pySrc).ToString());
        }

        [Test]
        public void Stmt_NestedDef()
        {
            var pySrc =
@"def foo():
    bar = 4

    " + "#" + @" inner fn should become C# lambda
    def baz(a, b):
        print (""Bar squared"" + bar * bar)
        return False

    baz('3', 4)
";
            var sExp =
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
            Assert.AreEqual(sExp, XlatMember(pySrc).ToString());
        }

        [Test]
        public void Stmt_foreach_tuple()
        {
            var pySrc =
@"for a,b in foo:
    print(a + b)
";
            var sExp =
@"foreach (var _tup_1 in foo) {
    a = _tup_1.Item1;
    b = _tup_1.Item2;
    Console.WriteLine(a + b);
}
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
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
    if (this.target_addr == null) {
        addr_strs.append(""default"");
    } else {
        addr_strs.append(String.Format(""%#x"", target_addr));
    }
}
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
        public void Stmt_Tuple_Assign()
        {
            var pySrc =
@"a,b = c,d
";
            var sExp =
@"a = c;
b = d;
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
        public void Stmt_Tuple_Assign_field()
        {
            var pySrc =
@"a.b, c.de = 'e','f'
";
            var sExp =
@"a.b = ""e"";
c.de = ""f"";
";
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Console.WriteLine(""[-] Can't open input file: "" + str(exc), file: sys.stderr);
        }
    }
}
";
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));

        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));

        }


        [Test]
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
            Assert.AreEqual(sExp, XlatStmts(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatMember(pySrc));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test(Description = "Reported in GitHub #15")]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test]
        public void Stmt_Async()
        {
            var pySrc =
            #region Expected
@"async def frobAsync():
    await frobber
";
            var sExp =
@"public static class testModule {
    
    public async static System.Threading.Tasks.Task<object> frobAsync() {
        await frobber;
    }
}
";
            #endregion
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }

        [Test(Description = "Reported in GitHub issue 29")]
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
            Assert.AreEqual(sExp, XlatModule(pySrc));
        }
    }
}
#endif
