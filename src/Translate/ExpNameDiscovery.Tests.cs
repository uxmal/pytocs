#if DEBUG
using Pytocs.Syntax;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    [TestFixture]
    public class ExpNameDiscoveryTests
    {
        private SymbolTable syms;

        [SetUp]
        public void Setup()
        {
            syms = new SymbolTable();
        }

        private void RunTest(string pyExp)
        {
            var lexer =  new Lexer("foo.py", new StringReader(pyExp));
            var parser = new Parser("foo.py", lexer);
            var exp = parser.test();
            exp.Accept(new ExpNameDiscovery(syms));
        }

        [Test]
        public void EN_NameReference()
        {
            RunTest("id");
            Assert.IsNotNull(syms.GetSymbol("id"));
        }
    }
}
#endif
