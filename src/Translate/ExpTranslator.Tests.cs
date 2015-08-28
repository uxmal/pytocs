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
using Pytocs.CodeModel;
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
    public class ExpTranslatorTests
    {
        string Xlat(string pyExp)
        {
            var rdr = new StringReader(pyExp);
            var lex = new Syntax.Lexer("foo.py", rdr);
            var par = new Syntax.Parser("foo.py", lex);
            var exp = par.test();
            Debug.Print("{0}", exp);
            var xlt = new ExpTranslator(new CodeGenerator(new CodeCompileUnit(), "", "test"));
            var csExp = exp.Accept(xlt);
            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
            pvd.GenerateCodeFromExpression(csExp, writer,
                new CodeGeneratorOptions
                {
                });
            return writer.ToString();
        }

        [Test]
        public void ExBinop()
        {
            Assert.AreEqual("a + b", Xlat("a + b"));
        }

        [Test]
        public void ExFn_NoArgs()
        {
            Assert.AreEqual("fn()", Xlat("fn()"));
        }

        [Test]
        public void ExIsNone()
        {
            Assert.AreEqual("a == null", Xlat("a is None"));
        }

        [Test]
        public void ExIsNotNone()
        {
            Assert.AreEqual("a != null", Xlat("a is not None"));
        }

        [Test]
        public void ExSelf()
        {
            Assert.AreEqual("this", Xlat("self"));
        }

        [Test]
        public void ExIn()
        {
            Assert.AreEqual(
@"new List<object> {
    ANCESTOR,
    EQUAL,
    PRECEDENT
}.Contains(this.compare(start))", Xlat("self.compare(start) in [ANCESTOR, EQUAL, PRECEDENT]"));
        }

        [Test]
        public void ExListComprehension()
        {
            string pySrc = "[int(x) for x in s]";
            string sExp = "s.Select(x => Convert.ToInt32(x))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        [Ignore]
        public void ExSubscript()
        {
            string pySrc = "x[2:]";
            string sExp = "x.Skip(2)";
            Assert.AreEqual(sExp, Xlat(pySrc));

        }

        [Test]
        public void ExListCompIf()
        {
            string pySrc = "[int(x) for x in s if x > 10]";
            string sExp = "s.Where(x => x > 10).Select(x => Convert.ToInt32(x))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExListCompWithSplit()
        {
            string pySrc = "[int(x) for x in s.split('/') if x != '']";
            string sExp = "s.split(\"/\").Where(x => x != \"\").Select(x => Convert.ToInt32(x))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExAnd()
        {
            string pySrc = "x & 3 and y & 40";
            string sExp = "x & 3 && y & 40";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExStringFormat()
        {
            string pySrc = "\"Hello %s%s\" % (world, '!')";
            string sExp = "String.Format(\"Hello %s%s\", world, \"!\")";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExNotIn()
        {
            string pySrc = "f not in [a, b]";
            string sExp =
@"!new List<object> {
    a,
    b
}.Contains(f)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExPow()
        {
            string pySrc = "a  ** b ** c";
            string sExp = "pow(a, pow(b, c))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExRealLiteral()
        {
            string pySrc = "0.1";
            string sExp = "0.1";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExListInitializer_SingleItem()
        {
            var pySrc = "['indices'] ";
            string sExp =
@"new List<object> {
    ""indices""
}";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void ExStringLiteral_Quotes()
        {
            var pySrc = "'\"'";
            string sExp = "\"\\\"\"";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_isinstance()
        {
            var pySrc = "isinstance(term, Foo)\r\n";
            string sExp = "term is Foo";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_not_isinstance()
        {
            var pySrc = "not isinstance(term, Foo)\r\n";
            string sExp = "!(term is Foo)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_ListFor()
        {
            var pySrc = "[int2byte(b) for b in bytelist]";
            string sExp = "bytelist.Select(b => int2byte(b))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_TestExpression()
        {
            var pySrc = "x if cond else y";
            string sExp = "cond ? x : y";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_CompFor()
        {
            var pySrc = "sum(int2byte(b) for b in bytelist)";
            string sExp = "sum(bytelist.Select(b => int2byte(b)))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_List()
        {
            var pySrc = "list(foo)";
            string sExp = "foo.ToList()";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_DictComprehension()
        {
            var pySrc = "{ k:copy.copy(v) for k, v in path.info.iteritems() }";
            string sExp = "path.info.iteritems().ToDictionary(k => k, v => copy.copy(v))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Regression1()
        {
            var pySrc = "((a, s) for a in stackframe.alocs.values() for s in a._segment_list)";
            string sExp = "stackframe.alocs.values().SelectMany(a => a._segment_list, (a,s) => Tuple.Create(a, s)).Select(a => Tuple.Create(a, s))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Regression2()
        {
            var pySrc = @"[""#"",""0"",r""\-"",r"" "",r""\+"",r""\'"",""I""]";
            var sExp = @"new List<object> {
    ""#"",
    ""0"",
    @""\-"",
    @"" "",
    @""\+"",
    @""\'"",
    ""I""
}";
            Assert.AreEqual(sExp, Xlat(pySrc));

        }

        [Test]
        public void Ex_Regression4()
        {
            var pySrc = "{ k:_raw_ast(a[k]) for k in a }";
            var sExp = "a.ToHashSet(k => _raw_ast(a[k]))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_SetComprehension()
        {
            var pySrc = "{ id(e) for e in self._breakpoints[t] }";
            var sExp = "this._breakpoints[t].ToHashSet(e => id(e))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }
    }
}
