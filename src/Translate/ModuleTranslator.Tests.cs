using Pytocs.CodeModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    [TestFixture]
    public class ModuleTranslatorTests
    {
        private string XlatModule(string pyModule)
        {
            var rdr = new StringReader(pyModule);
            var lex = new Syntax.Lexer("foo.py", rdr);
            var par = new Syntax.Parser("foo.py", lex);
            var stm = par.Parse();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "module");
            var xlt = new ModuleTranslator(gen);
            xlt.Translate(stm);

            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
            pvd.GenerateCodeFromCompileUnit(unt, writer, new CodeGeneratorOptions { });
            return writer.ToString();
        }

        [Test]
        public void Module_UsesList()
        {
            var pyModule =
@"st = [ 'a' ]
";
            var sExp =
@"namespace test {
    
    using System.Collections.Generic;
    
    public static class module {
        
        public static object st = new List<object> {
            ""a""
        };
    }
}
";
            Assert.AreEqual(sExp, XlatModule(pyModule));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyModule));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyModule));
        }

        [Test]
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
            Assert.AreEqual(sExp, XlatModule(pyModule));

        }

        [Test]
        public void Module_MemberVars()
        {
            var pyModule =
@"_tokenizer = antlrre.Tokenizer(
    # token regular expression table
    tokens = [
        (foo.a, 'sss')
    ])
";
            var sExp =
@"namespace test {
    
    using System.Collections.Generic;
    
    public static class module {
        
        public static object _tokenizer = antlrre.Tokenizer(tokens = new List<object> {
            Tuple.Create(foo.a, ""sss"")
        });
    }
}
";
            Assert.AreEqual(sExp, XlatModule(pyModule));
        }
    }
}