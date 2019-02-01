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
using System.Threading;

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
            var opt = new Dictionary<string, object> { { "quiet", true } };
            var ana = new AnalyzerImpl(fs, logger, opt, DateTime.Now);
            var mod = new Module(
                "module",
                new SuiteStatement(stm, filename, 0, 0),
                filename, 0, 0);
            ana.LoadModule(mod);
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
            Tuple.Create(foo.a, ""sss"")
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
        
        public static object static_func() {
        }
    }
    
    public class MyClass {
        
        public virtual object method(object arg) {
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
            
            public Point(object x, object y) {
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
            
            public virtual object frob() {
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
    }
}
#endif
