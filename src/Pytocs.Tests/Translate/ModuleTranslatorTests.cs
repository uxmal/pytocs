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

#if DEBUG
using Pytocs.Core;
using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using Pytocs.Core.TypeInference;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Pytocs.UnitTests.Translate
{
    public class ModuleTranslatorTests
    {
        private IFileSystem fs;
        private ILogger logger;

        public ModuleTranslatorTests()
        {
            this.fs = new FakeFileSystem();
            this.logger = new FakeLogger();
        }

        private string XlatModule(string pyModule, string filename = "module.py")
        {
            var rdr = new StringReader(pyModule);
            var lex = new Lexer(filename, rdr);
            var par = new Parser(filename, lex);
            var stm = par.Parse().ToList();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", Path.GetFileNameWithoutExtension(filename));
            var opt = new Dictionary<string, object> { { "--quiet", true } };
            var ana = new AnalyzerImpl(fs, logger, opt, DateTime.Now);
            var mod = new Module(
                "module",
                new SuiteStatement(stm, filename, 0, 0),
                filename, 0, 0);
            ana.LoadModule(mod);
            ana.ApplyUncalled();
            
            var types = new TypeReferenceTranslator(ana.BuildTypeDictionary());
            var xlt = new ModuleTranslator(types, gen);
            xlt.Translate(stm);

            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
            pvd.GenerateCodeFromCompileUnit(unt, writer, new CodeGeneratorOptions { });
            return writer.ToString();
        }

        [Fact(DisplayName = nameof(Module_UsesList))]
        public void Module_UsesList()
        {
            var pyModule =
@"st = [ 'a' ]
";
            var sExp =
@"namespace test {
    
    using System.Collections.Generic;
    
    public static class module {
        
        public static List<string> st = new List<string> {
            ""a""
        };
    }
}
";
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_ComplexAssignment))]
        public void Module_ComplexAssignment()
        {
            var pyModule =
@"ax.ay = 'AAZZ'
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        static module() {
            ax.ay = ""AAZZ"";
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_StandaloneFn))]
        public void Module_StandaloneFn()
        {
            var pyModule =
@"parse.foo()
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        static module() {
            parse.foo();
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_DocComment))]
        public void Module_DocComment()
        {
            var pyModule =
@"'''Doc comment in
two lines
'''
";
            var sExp =
@"// Doc comment in
// two lines
// 
namespace test {
    
    public static class module {
    }
}
";
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_MemberVars))]
        public void Module_MemberVars()
        {
            var pyModule =
@"_tokenizer = antlrre.Tokenizer(
    " + @"# token regular expression table
    tokens = [
        (foo.a, 'sss')
    ])
";
            var sExp =
@"namespace test {
    
    using System;
    
    using System.Collections.Generic;
    
    public static class module {
        
        public static object _tokenizer = antlrre.Tokenizer(tokens: new List<Tuple<object, string>> {
            (foo.a, ""sss"")
        });
    }
}
";
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module__init__file))]
        public void Module__init__file()
        {
            var pyModule =
@"
def static_func():
  pass;

class MyClass:
   def method(self, arg):
       print(arg)
";
            string sExp =
@"namespace test {
    
    public static class @__init__ {
        
        public static void static_func() {
        }
    }
    
    public class MyClass {
        
        public virtual void method(object arg) {
            Console.WriteLine(arg);
        }
    }
}
";
            Debug.Print(XlatModule(pyModule, "__init__.py"));

            Assert.Equal(sExp, XlatModule(pyModule, "__init__.py"));
        }

        [Fact(DisplayName = nameof(Module__init__nested_classes))]
        public void Module__init__nested_classes()
        {
            var pyModule =
@"
class OuterClass:
   class InnerClass:
       pass
";
            string sExp =
@"namespace test {
    
    public static class @__init__ {
    }
    
    public class OuterClass {
        
        public class InnerClass {
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pyModule, "__init__.py"));
        }

        [Fact(DisplayName = nameof(Module_import_as))]
        public void Module_import_as()
        {
            var pyModule =
@"
import vivisect.const as viv_const
";
            var sExp =
@"namespace test {
    
    using viv_const = vivisect.@const;
    
    public static class module {
    }
}
";
            Debug.Print(XlatModule(pyModule));
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_init_global))]
        public void Module_init_global()
        {
            var pyModule =
@"
d = {}
";
            var sExp =
@"namespace test {
    
    using System.Collections.Generic;
    
    public static class module {
        
        public static Dictionary<object, object> d = new Dictionary<object, object> {
        };
    }
}
";
            Debug.Print(XlatModule(pyModule));
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_point_class_members))]
        public void Module_point_class_members()
        {
            var pyModule =
@"
class Point:
    def __init__(self, x, y):
        self.x = x;
        self.y = y;

pt = Point(3.5, -0.4)
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        public class Point {
            
            public object x;
            
            public object y;
            
            public Point(double x, double y) {
                this.x = x;
                this.y = y;
            }
        }
        
        public static Point pt = new Point(3.5, -0.4);
    }
}
";
            Debug.Print(XlatModule(pyModule));
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_slots))]
        public void Module_slots()
        {
            var pyModule =
@"
class Point:
    __slots__ = 'x','y'
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        public class Point {
            
            public object x;
            
            public object y;
        }
    }
}
";
            Debug.Print(XlatModule(pyModule));
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_slots))]
        public void Module_slots_with_types()
        {
            var pyModule =
@"
class Frob:
    __slots__ = 'a','b'

    def frob(self):
        self.a = 'Hello'
        self.b = 42
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        public class Frob {
            
            public string a;
            
            public int b;
            
            public virtual void frob() {
                this.a = ""Hello"";
                this.b = 42;
            }
        }
    }
}
";
            Debug.Print(XlatModule(pyModule));
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact(DisplayName = nameof(Module_issue_61))]
        public void Module_issue_61()
        {
            var pyModule = @"
class TestClass:
    def TestFunction(self):
        return TestValue(
            {
                **something
            }
        )
";
            var sExp =
@"namespace test {
    
    using pytocs.runtime;
    
    public static class module {
        
        public class TestClass {
            
            public virtual object TestFunction() {
                return TestValue(DictionaryUtils.Unpack<string, object>(something));
            }
        }
    }
}
";
            Debug.Print(XlatModule(pyModule));
            Assert.Equal(sExp, XlatModule(pyModule));
        }

        [Fact]
        public void Stmt_super_call()
        {
            var pySrc =
@"class Foo(Bar):
    def froz(self):
        super(Bar,self).froz()
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        public class Foo
            : Bar {
            
            public virtual void froz() {
                base.froz();
            }
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_super_call_multiple_base_classes()
        {
            var pySrc =
@"class Foo(Bar1, Bar2):
    def froz(self):
        super(Bar2,self).froz()
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        public class Foo
            : Bar1, Bar2 {
            
            public virtual void froz() {
                ((Bar2) this).froz();
            }
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }


        [Fact(Skip = "Not ready yet")]
        public void Module_void_function()
        {
            var pySrc =
@"def test(s):
    print(s)
";
            var sExp = 
@"namespace test {
    
    public static class module {
        
        public static void test(object s) {
            Console.Write(s);
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Stmt_super_outside_class()
        {
            var pySrc =
@"def __ELFSymbolTypeArchParser(cls, value):
    if isinstance(value, int):
        return super(ELFSymbolType, cls).__new__(cls, (value, None))
    else:
        return super(ELFSymbolType, cls).__new__(cls, value)
";

            var sExp =
@"namespace test {
    
    public static class module {
        
        public static object @__ELFSymbolTypeArchParser(object cls, object value) {
            if (value is int) {
                return ((ELFSymbolType) this).@__new__(cls, (value, null));
            } else {
                return ((ELFSymbolType) this).@__new__(cls, value);
            }
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact(DisplayName = nameof(Func_Default_Args))]
        public void Func_Default_Args()
        {
            var pySrc =
@"def foo(i=0,s='empty',f=True):
    return (f, i, s)
";

            var sExp =
@"namespace test {
    
    public static class module {
        
        public static Tuple<bool, int, string> foo(int i = 0, string s = ""empty"", bool f = true) {
            return (f, i, s);
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact]
        public void Module_TopLevelAssignment()
        {
            var pySrc =
@"a, b, c = 'a', ""b"", 'c'
";
            var sExp =
@"namespace test {
    
    public static class module {
        
        static module() {
            a = ""a"";
            b = ""b"";
            c = ""c"";
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }

        [Fact(DisplayName = nameof(Module_non_assignments))]
        public void Module_non_assignments()
        {
            var pySrc = @"
num = 8

$ To take the input from the user
$num = float(input('Enter a number: '))

num_sqrt = num ** 0.5
print('The square root of %0.3f is %0.3f' % (num, num_sqrt))
".Replace("$","#");
            var sExp =
@"namespace test {
    
    using System;
    
    public static class module {
        
        public static int num;
        
        public static double num_sqrt;
        
        static module() {
            num = 8;
            // To take the input from the user
            //num = float(input('Enter a number: '))
            num_sqrt = Math.Pow(num, 0.5);
            Console.WriteLine(String.Format(""The square root of %0.3f is %0.3f"", num, num_sqrt));
        }
    }
}
";
            Assert.Equal(sExp, XlatModule(pySrc));
        }


        [Fact(DisplayName = nameof(Module_Enum))]
        public void Module_Enum()
        {
            var pySrc = @"
class JobType(Enum):
    """"""
    This is a comment
    """"""

    NORMAL = 0
    FAST = 1
    SLOW = 2
    DATAREF_HINTS = 4
";
            var sExpected =
@"namespace test {
    
    public static class module {
        
        // 
        //     This is a comment
        //     
        public enum JobType {
            
            NORMAL = 0,
            
            FAST = 1,
            
            SLOW = 2,
            
            DATAREF_HINTS = 4,
        }
    }
}
";
            Assert.Equal(sExpected, XlatModule(pySrc));
        }
    }
}
#endif
