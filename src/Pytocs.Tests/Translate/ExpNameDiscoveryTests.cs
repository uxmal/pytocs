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
using Pytocs.Core.Syntax;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.Translate;
using Pytocs.Core;

namespace Pytocs.UnitTests.Translate
{
    public class ExpNameDiscoveryTests
    {
        private SymbolTable syms;

        public ExpNameDiscoveryTests()
        {
            syms = new SymbolTable();
        }

        private void RunTest(string pyExp)
        {
            var lexer =  new Lexer("foo.py", new StringReader(pyExp));
            var parser = new Parser("foo.py", lexer);
            var exp = parser.test();
            exp?.Accept(new ExpNameDiscovery(syms));
        }

        [Fact]
        public void EN_NameReference()
        {
            RunTest("id");
            Assert.NotNull(syms.GetSymbol("id"));
        }
    }
}
#endif
