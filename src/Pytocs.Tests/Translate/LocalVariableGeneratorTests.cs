﻿#region License

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

#if DEBUG
using Xunit;
using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.Translate;

namespace Pytocs.UnitTests.Translate
{
    public class LocalVariableTranslator
    {
        public LocalVariableTranslator()
        {
        }

        private string XlatModule(string pyModule)
        {
            var rdr = new StringReader(pyModule);
            var lex = new Lexer("foo.py", rdr);
            var par = new Parser("foo.py", lex);
            var stm = par.stmt();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var sym = new SymbolGenerator();
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new StatementTranslator(null, types, gen, sym, new HashSet<string>());
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

        private string XlatMember(string pyModule)
        {
            var rdr = new StringReader(pyModule);
            var lex = new Lexer("foo.py", rdr);
            var par = new Parser("foo.py", lex);
            var stm = par.stmt();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, "test", "testModule");
            var sym = new SymbolGenerator();
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new StatementTranslator(null, types, gen, sym, new HashSet<string>());
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

        [Fact]
        public void Lvt_IfElseDeclaration()
        {
            var pySrc =
@"def foo():
    if self.x:
        y = 3
    else:
        y = 9
    self.y = y * 2
";

            var sExp =
@"public static object foo() {
    object y;
    if (this.x) {
        y = 3;
    } else {
        y = 9;
    }
    this.y = y * 2;
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }

        [Fact]
        public void Lvi_ForceStandAloneDefinition()
        {
            var pySrc =
@"def foo():
    if self.x:
        x = self.x
    x = x + 1
";

            var sExp =
@"public static object foo() {
    object x;
    if (this.x) {
        x = this.x;
    }
    x = x + 1;
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }

        [Fact]
        public void Lvi_LocalRedefinition()
        {
            var pySrc =
@"def foo():
    if self.x:
        x = self.x
        x = x + 1
        self.x = x
";

            var sExp =
@"public static object foo() {
    if (this.x) {
        var x = this.x;
        x = x + 1;
        this.x = x;
    }
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }

        [Fact]
        public void Lvi_LocalInBranch()
        {
            var pySrc =
@"def foo():
    if self.x:
        x = self.x
        self.x = None
        x.foo()
";

            var sExp =
@"public static object foo() {
    if (this.x) {
        var x = this.x;
        this.x = null;
        x.foo();
    }
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }

        [Fact]
        public void Lvi_ModifyParameter()
        {
            var pySrc =
@"def foo(frog):
    if frog is None:
        frog = 'default'
    bar(frog)
";

            var sExp =
@"public static object foo(object frog) {
    if (frog == null) {
        frog = ""default"";
    }
    bar(frog);
}

";
            Assert.Equal(sExp, XlatMember(pySrc).ToString());
        }
    }
}
#endif