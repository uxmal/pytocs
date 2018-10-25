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
using NUnit.Framework;
using Pytocs.CodeModel;
using Pytocs.Syntax;
using Pytocs.Types;
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
        private string nl = Environment.NewLine;

        string Xlat(string pyExp)
        {
            var rdr = new StringReader(pyExp);
            var lex = new Syntax.Lexer("foo.py", rdr);
            var par = new Syntax.Parser("foo.py", lex);
            var exp = par.test();
            Debug.Print("{0}", exp);
            var sym = new SymbolGenerator();
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new ExpTranslator(types, new CodeGenerator(new CodeCompileUnit(), "", "test"), sym);
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
        public void ExIsOneOfManyTypes()
        {
            var pysrc = "isinstance(read_addr, (int, long))";
            var sExp = "read_addr is int || read_addr is long";
            Assert.AreEqual(sExp, Xlat(pysrc));
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
            string sExp = "Math.Pow(a, Math.Pow(b, c))";
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
            string sExp = "bytelist.Select(b => int2byte(b)).Sum()";
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
            var sExp = "a.ToDictionary(k => k, k => _raw_ast(a[k]))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_SetComprehension()
        {
            var pySrc = "{ id(e) for e in self._breakpoints[t] }";
            var sExp = "this._breakpoints[t].ToHashSet(e => id(e))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }


        [Test]
        public void Ex_SetComprehension2()
        {
            var pySrc = "{(a.addr,b.addr) for a,b in fdiff.block_matches}";
            var sExp = "fdiff.block_matches.Chop((a,b) => Tuple.Create(a.addr, b.addr)).ToHashSet()";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_SetComprehension3()
        {
            var pySrc = "{(a.addr ,b.addr) for a,b in fdiff.block_matches}";
            var sExp = "fdiff.block_matches.Chop((a,b) => Tuple.Create(a.addr, b.addr)).ToHashSet()";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_DictComprehension1()
        {
            var pySrc = "{ k:copy.copy(v) for k, v in path.info.iteritems() }";
            string sExp = "path.info" +
                ".ToDictionary(_tup_1 => _tup_1.Item1, _tup_1 => copy.copy(_tup_1.Item2))";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_DictComprehension2()
        {
            var pySrc = "{ k:k + 'X' for k in path.info }";
            string sExp = "path.info.ToDictionary(k => k, k => k + \"X\")";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_DictComprehension3()
        {
            var pySrc = "{ a + b: b + c for a, b, c in path }";
            string sExp = "path" +
                ".ToDictionary(_tup_1 => _tup_1.Item1 + _tup_1.Item2, _tup_1 => _tup_1.Item2 + _tup_1.Item3)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_NamedParameterCall()
        {
            var pySrc = "foo(bar='baz')";
            var sExp = "foo(bar: \"baz\")";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_HashSet()
        {
            var pySrc = "set()";
            var sExp = "new HashSet<object>()";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_HashTable()
        {
            var pySrc = "{}";
            var sExp = "new Dictionary<object, object> {" + nl + "}";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Regression5()
        {
            var pySrc = "round(float( float(count) / float(self.insn_count)), 3) >= .67";
            var sExp = "round(float(float(count) / float(this.insn_count)), 3) >= 0.67";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_ByteConstant()
        {
            var pySrc = "b'\\xfe\\xed' * init_stack_size";
            var sExp = "new byte[] { 0xfe, 0xed } * init_stack_size";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_intrinsic_len()
        {
            var pysrc = "len(foo.bar)";
            var sExp = "foo.bar.Count";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_instrinsic_iteritems()
        {
            var pysrc = "foo.iteritems()";
            var sExp = "foo";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_instrinsic_itervalues()
        {
            var pysrc = "foo.itervalues()";
            var sExp = "foo.Values";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_instrinsic_iterkeys()
        {
            var pysrc = "foo.iterkeys()";
            var sExp = "foo.Keys";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_instrinsic_sum()
        {
            var pysrc = "sum(bar)";
            var sExp = "bar.Sum()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }
        
        [Test]
        public void Ex_instrinsic_list()
        {
            var pysrc = "list()";
            var sExp = "new List<object>()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_instrinsic_set()
        {
            var pysrc = "set()";
            var sExp = "new HashSet<object>()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_instrinsic_set_with_args()
        {
            var pysrc = "set(a)";
            var sExp = "new HashSet<object>(a)";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_dict()
        {
            var pysrc = "dict()";
            var sExp = "new Dictionary<object, object>()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_dict_kwargs()
        {
            var pysrc = "dict(foo='bob', bar=sue+3)";
            var sExp =
                "new Dictionary<@string, object> {" + nl +
                "    {" + nl +
                "        \"foo\"," + nl +
                "        \"bob\"}," + nl +
                "    {" + nl +
                "        \"bar\"," + nl +
                "        sue + 3}}";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_dict_iterable()
        {
            var pysrc = "dict(a)";
            var sExp = "a.ToDictionary()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_filter()
        {
            var pysrc = "filter(fn, items)";
            var sExp = "items.Where(fn).ToList()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_filter_none()
        {
            var pysrc = "filter(None, items)";
            var sExp = "items.Where(_p_1 => _p_1 != null).ToList()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_sorted()
        {
            var pysrc = "sorted(items)";
            var sExp = "items.OrderBy(_p_1 => _p_1).ToList()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_sorted_cmp()
        {
            var pysrc = "sorted(items, cmp)";
            var sExp = "items.OrderBy(cmp).ToList()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_sorted_key()
        {
            var pysrc = "sorted(items, key=lambda x: x.addr)";
            var sExp = "items.OrderBy(x => x.addr).ToList()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_sorted_reverse_bool()
        {
            var pysrc = "sorted(items, reverse=true)";
            var sExp = "items.OrderBy(_p_1 => _p_1).ToList()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_intrinsic_enumerate()
        {
            var pysrc = "enumerate(items)";
            var sExp = "items.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_for_with_if()
        {
            var pysrc = "(a for a in function.endpoints if a.addr == endpoint_addr)";
            var sExp = "function.endpoints.Where(a => a.addr == endpoint_addr)";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_for_with_if_projected()
        {
            var pysrc = "(a.x for a in function.endpoints if a.addr == endpoint_addr)";
            var sExp = "function.endpoints.Where(a => a.addr == endpoint_addr).Select(a => a.x)";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_for_with_if_projected_to_set()
        {
            var pysrc = "{a for a in function.endpoints if a.addr == endpoint_addr}";
            var sExp = "function.endpoints.Where(a => a.addr == endpoint_addr).ToHashSet()";
            Assert.AreEqual(sExp, Xlat(pysrc));
        }

        [Test]
        public void Ex_dict_Comprehension()
        {
            var pySrc = "{AT.from_rva(v, self).to_mva(): k for (k, v) in self._plt.iteritems()}";
            var sExp = "this._plt.ToDictionary(_de1 => AT.from_rva(_de1.Value, this).to_mva(), _de1 => _de1.Key)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_struct_unpack()
        {
            var pySrc = "struct.unpack('3x', buffer)";
            var sExp = "@struct.unpack(\"3x\", buffer)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_ScientificNotation()
        {
            var pySrc = "1E-5";
            var sExp = "1E-05";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_SmallInteger()
        {
            var pySrc = "128";
            var sExp = "128";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Github_Issue_14()
        {
            var pySrc = "len(x + y + z)";
            var sExp = "(x + y + z).Count";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test(Description = "Reported in Github issue #17")]
        public void Ex_Associativity()
        {
            string pySrc = "a - (b + c)";
            string sExp = "a - (b + c)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test(Description = "Reported in Github issue #26")]
        public void Ex_Infinity()
        {
            string pySrc = "-1e3000000";
            string sExp = "double.NegativeInfinity";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Infinity_FloatBif()
        {
            string pySrc = "float('+inf')";
            string sExp = "double.PositiveInfinity";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Complex_Literal()
        {
            string pySrc = "3 - 4j";
            string sExp = "new Complex(3.0, -4.0)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Complex()
        {
            string pySrc = "complex(3,4)";
            string sExp = "new Complex(3, 4)";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }

        [Test]
        public void Ex_Await()
        {
            string pySrc = "await foo";
            string sExp = "await foo";
            Assert.AreEqual(sExp, Xlat(pySrc));
        }
    }
}
#endif
