#region License

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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace Pytocs.UnitTests.Translate
{
    public class ExpTranslatorTests
    {
        private readonly string nl = Environment.NewLine;

        private string Xlat(string pyExp)
        {
            StringReader rdr = new StringReader(pyExp);
            Lexer lex = new Lexer("foo.py", rdr);
            Parser par = new Parser("foo.py", lex);
            Exp exp = par.test();
            Debug.Print("{0}", exp);
            SymbolGenerator sym = new SymbolGenerator();
            TypeReferenceTranslator types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            ExpTranslator xlt =
                new ExpTranslator(null, types, new CodeGenerator(new CodeCompileUnit(), "", "test"), sym);
            CodeExpression csExp = exp.Accept(xlt);
            CSharpCodeProvider pvd = new CSharpCodeProvider();
            StringWriter writer = new StringWriter();
            pvd.GenerateCodeFromExpression(csExp, writer,
                new CodeGeneratorOptions());
            return writer.ToString();
        }

        [Theory]
        [InlineData("range(10)", "Enumerable.Range(0, 10)")]
        [InlineData("range(10 + a)", "Enumerable.Range(0, 10 + a)")]
        [InlineData("range(2, 10)", "Enumerable.Range(2, 10 - 2)")]
        [InlineData("range(2 + x, 10)", "Enumerable.Range(2 + x, 10 - (2 + x))")]
        [InlineData("range(2, 10 + y)", "Enumerable.Range(2, 10 + y - 2)")]
        [InlineData("range(2, 10, 2)",
            "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 - 2) / 2))).Select(_x_1 => 2 + _x_1 * 2)")]
        [InlineData("range(2, 10, 3 + a);",
            "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 - 2) / (3 + a)))).Select(_x_1 => 2 + _x_1 * (3 + a))")]
        [InlineData("range(2, 10 + b, 5);",
            "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 + b - 2) / 5))).Select(_x_1 => 2 + _x_1 * 5)")]
        [InlineData("range(2 + c, 10, 20);",
            "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 - (2 + c)) / 20))).Select(_x_1 => 2 + c + _x_1 * 20)")]
        public void Ex_range(string pyStm, string sExp)
        {
            string x = Xlat(pyStm);
            Assert.Equal(sExp, x);
        }

        // Reported in Github issue #17
        [Fact]
        public void Ex_Associativity()
        {
            string pySrc = "a - (b + c)";
            string sExp = "a - (b + c)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Await()
        {
            string pySrc = "await foo";
            string sExp = "await foo";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_ByteConstant()
        {
            string pySrc = "b'\\xfe\\xed' * init_stack_size";
            string sExp = "new byte[] { 0xfe, 0xed } * init_stack_size";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_CompFor()
        {
            string pySrc = "sum(int2byte(b) for b in bytelist)";
            string sExp =
                @"(from b in bytelist
    select int2byte(b)).Sum()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Complex()
        {
            string pySrc = "complex(3,4)";
            string sExp = "new Complex(3, 4)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Complex_Literal()
        {
            string pySrc = "3 - 4j";
            string sExp = "new Complex(3.0, -4.0)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_dict_Comprehension()
        {
            string pySrc = "{AT.from_rva(v, self).to_mva(): k for (k, v) in self._plt.iteritems()}";
            string sExp = "this._plt.ToDictionary(_de1 => AT.from_rva(_de1.Value, this).to_mva(), _de1 => _de1.Key)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_DictComprehension1()
        {
            string pySrc = "{ k:copy.copy(v) for k, v in path.info.iteritems() }";
            string sExp = "path.info" +
                          ".ToDictionary(_tup_1 => _tup_1.Item1, _tup_1 => copy.copy(_tup_1.Item2))";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_DictComprehension2()
        {
            string pySrc = "{ k:k + 'X' for k in path.info }";
            string sExp = "path.info.ToDictionary(k => k, k => k + \"X\")";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_DictComprehension3()
        {
            string pySrc = "{ a + b: b + c for a, b, c in path }";
            string sExp = "path" +
                          ".ToDictionary(_tup_1 => _tup_1.Item1 + _tup_1.Item2, _tup_1 => _tup_1.Item2 + _tup_1.Item3)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_for_with_if()
        {
            string pysrc = "(a for a in function.endpoints if a.addr == endpoint_addr)";
            string sExp =
                @"from a in function.endpoints
    where a.addr == endpoint_addr
    select a";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_for_with_if_projected()
        {
            string pysrc = "(a.x for a in function.endpoints if a.addr == endpoint_addr)";
            string sExp =
                @"from a in function.endpoints
    where a.addr == endpoint_addr
    select a.x";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_for_with_if_projected_to_set()
        {
            string pysrc = "{a for a in function.endpoints if a.addr == endpoint_addr}";
            string sExp =
                @"(from a in function.endpoints
    where a.addr == endpoint_addr
    select a).ToHashSet()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_Github_Issue_14()
        {
            string pySrc = "len(x + y + z)";
            string sExp = "(x + y + z).Count";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_HashSet()
        {
            string pySrc = "set()";
            string sExp = "new HashSet<object>()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_HashTable()
        {
            string pySrc = "{}";
            string sExp = "new Dictionary<object, object> {" + nl + "}";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_hex_literal()
        {
            string pySrc = "0x_1234";
            string sExp = "0x_1234";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        // Reported in Github issue #26
        [Fact]
        public void Ex_Infinity()
        {
            string pySrc = "-1e3000000";
            string sExp = "double.NegativeInfinity";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Infinity_FloatBif()
        {
            string pySrc = "float('+inf')";
            string sExp = "double.PositiveInfinity";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_instrinsic_iteritems()
        {
            string pysrc = "foo.iteritems()";
            string sExp = "foo";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact(DisplayName = nameof(Ex_instrinsic_iterkeys))]
        public void Ex_instrinsic_iterkeys()
        {
            string pysrc = "foo.iterkeys()";
            string sExp = "foo.Keys";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_itervalues()
        {
            string pysrc = "foo.itervalues()";
            string sExp = "foo.Values";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_list()
        {
            string pysrc = "list()";
            string sExp = "new List<object>()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_set()
        {
            string pysrc = "set()";
            string sExp = "new HashSet<object>()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_set_with_args()
        {
            string pysrc = "set(a)";
            string sExp = "new HashSet<object>(a)";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_sum()
        {
            string pysrc = "sum(bar)";
            string sExp = "bar.Sum()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_dict()
        {
            string pysrc = "dict()";
            string sExp = "new Dictionary<object, object>()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_dict_iterable()
        {
            string pysrc = "dict(a)";
            string sExp = "a.ToDictionary()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_dict_kwargs()
        {
            string pysrc = "dict(foo='bob', bar=sue+3)";
            string sExp =
                "new Dictionary<@string, object> {" + nl +
                "    {" + nl +
                "        \"foo\"," + nl +
                "        \"bob\"}," + nl +
                "    {" + nl +
                "        \"bar\"," + nl +
                "        sue + 3}}";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_enumerate()
        {
            string pysrc = "enumerate(items)";
            string sExp = "items.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_filter()
        {
            string pysrc = "filter(fn, items)";
            string sExp = "items.Where(fn).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_filter_none()
        {
            string pysrc = "filter(None, items)";
            string sExp = "items.Where(_p_1 => _p_1 != null).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_len()
        {
            string pysrc = "len(foo.bar)";
            string sExp = "foo.bar.Count";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted()
        {
            string pysrc = "sorted(items)";
            string sExp = "items.OrderBy(_p_1 => _p_1).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted_cmp()
        {
            string pysrc = "sorted(items, cmp)";
            string sExp = "items.OrderBy(cmp).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted_key()
        {
            string pysrc = "sorted(items, key=lambda x: x.addr)";
            string sExp = "items.OrderBy(x => x.addr).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted_reverse_bool()
        {
            string pysrc = "sorted(items, reverse=true)";
            string sExp = "items.OrderBy(_p_1 => _p_1).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_isinstance()
        {
            string pySrc = "isinstance(term, Foo)\r\n";
            string sExp = "term is Foo";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_issue_57))]
        public void Ex_issue_57()
        {
            string pySrc = "{'a': 'str', **kwargs }";
            string sExp = @"DictionaryUtils.Unpack<string, object>((""a"", ""str""), kwargs)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_lambda_kwargs()
        {
            string pySrc = "lambda x, **k: xpath_text(hd_doc, './/video/' + x, **k)";
            string sExp = "(x,k) => xpath_text(hd_doc, \".//video/\" + x, k)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_List()
        {
            string pySrc = "list(foo)";
            string sExp = "foo.ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_list_unpackers()
        {
            string pySrc = "[ 1, 2, *foo, 3, 4]";
            string sExp =
                @"ListUtils.Unpack<object>(new object[] {
    1,
    2,
}, foo, new object[] {
    3,
    4,
})";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_ListComprehension_Alternating_fors()
        {
            string pySrc =
                "[state for (stash, states) in self.simgr.stashes.items() if stash != 'pruned' for state in states]";
            string sExp =
                @"(from _tup_1 in this.simgr.stashes.items().Chop((stash,states) => (stash, states))
    let stash = _tup_1.Item1
    let states = _tup_1.Item2
    where stash != ""pruned""
    from state in states
    select state).ToList()";

            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_ListFor()
        {
            string pySrc = "[int2byte(b) for b in bytelist]";
            string sExp =
                @"(from b in bytelist
    select int2byte(b)).ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_NamedParameterCall()
        {
            string pySrc = "foo(bar='baz')";
            string sExp = "foo(bar: \"baz\")";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        //$TODO: this is not strictly correct, but at least it doesn't crash anymore
        [Fact(DisplayName = nameof(Ex_nested_for_comprehension))]
        public void Ex_nested_for_comprehension()
        {
            string pySrc = "((a, b) for a,b in list for a,b in (a,b)))";
            string sExp =
                @"from _tup_1 in list.Chop((a,b) => (a, b))
    let a = _tup_1.Item1
    let b = _tup_1.Item2
    from _tup_2 in (a, b).Chop((a,b) => (a, b))
    let a = _tup_2.Item1
    let b = _tup_2.Item2
    select (a, b)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_nested_for_comprehensions()
        {
            string pySrc = "[(a, s) for a in stackframe.alocs.values() for s in a._segment_list]";
            string sExp =
                @"(from a in stackframe.alocs.values()
    from s in a._segment_list
    select (a, s)).ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_not_isinstance()
        {
            string pySrc = "not isinstance(term, Foo)\r\n";
            string sExp = "!(term is Foo)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Regression2()
        {
            string pySrc = @"[""#"",""0"",r""\-"",r"" "",r""\+"",r""\'"",""I""]";
            string sExp = @"new List<object> {
    ""#"",
    ""0"",
    @""\-"",
    @"" "",
    @""\+"",
    @""\'"",
    ""I""
}";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Regression4()
        {
            string pySrc = "{ k:_raw_ast(a[k]) for k in a }";
            string sExp = "a.ToDictionary(k => k, k => _raw_ast(a[k]))";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Regression5()
        {
            string pySrc = "round(float( float(count) / float(self.insn_count)), 3) >= .67";
            string sExp = "round(float(float(count) / float(this.insn_count)), 3) >= 0.67";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_ScientificNotation()
        {
            string pySrc = "1E-5";
            string sExp = "1E-05";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_set_unpackers()
        {
            string pySrc = "{ *set1, 0, 1, *set2, '3' }";
            string sExp = "SetUtils.Unpack<object>(set1, new object[] {" + nl +
                          "    0," + nl +
                          "    1," + nl +
                          "}, set2, new object[] {" + nl +
                          "    \"3\"," + nl +
                          "})";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_SetComprehension()
        {
            string pySrc = "{ id(e) for e in self._breakpoints[t] }";
            string sExp =
                @"(from e in this._breakpoints[t]
    select id(e)).ToHashSet()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_SetComprehension2()
        {
            string pySrc = "{(a.addr,b.addr) for a,b in fdiff.block_matches}";
            string sExp =
                @"(from _tup_1 in fdiff.block_matches.Chop((a,b) => (a, b))
    let a = _tup_1.Item1
    let b = _tup_1.Item2
    select (a.addr, b.addr)).ToHashSet()";

            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_SmallInteger()
        {
            string pySrc = "128";
            string sExp = "128";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_str_builtin))]
        public void Ex_str_builtin()
        {
            string pySrc = "str(a)";
            string sExp = "a.ToString()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_str_builtin_encoding))]
        public void Ex_str_builtin_encoding()
        {
            string pySrc = "str(a, 'utf-8')";
            string sExp = "Encoding.GetEncoding(\"utf-8\").GetString(a)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_struct_unpack()
        {
            string pySrc = "struct.unpack('3x', buffer)";
            string sExp = "@struct.unpack(\"3x\", buffer)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_TestExpression()
        {
            string pySrc = "x if cond else y";
            string sExp = "cond ? x : y";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExAnd()
        {
            string pySrc = "x & 3 and y & 40";
            string sExp = "x & 3 && y & 40";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(ExBinop))]
        public void ExBinop()
        {
            Assert.Equal("a + b", Xlat("a + b"));
        }

        [Fact]
        public void ExFn_NoArgs()
        {
            Assert.Equal("fn()", Xlat("fn()"));
        }

        [Fact]
        public void ExIn()
        {
            Assert.Equal(
                @"new List<object> {
    ANCESTOR,
    EQUAL,
    PRECEDENT
}.Contains(this.compare(start))", Xlat("self.compare(start) in [ANCESTOR, EQUAL, PRECEDENT]"));
        }

        [Fact]
        public void ExIsNone()
        {
            Assert.Equal("a == null", Xlat("a is None"));
        }

        [Fact]
        public void ExIsNotNone()
        {
            Assert.Equal("a != null", Xlat("a is not None"));
        }

        [Fact]
        public void ExIsOneOfManyTypes()
        {
            string pysrc = "isinstance(read_addr, (int, long))";
            string sExp = "read_addr is int || read_addr is long";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void ExListCompIf()
        {
            string pySrc = "[int(x) for x in s if x > 10]";
            string sExp =
                @"(from x in s
    where x > 10
    select Convert.ToInt32(x)).ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExListComprehension()
        {
            string pySrc = "[int(x) for x in s]";
            string sExp =
                @"(from x in s
    select Convert.ToInt32(x)).ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExListCompWithSplit()
        {
            string pySrc = "[int(x) for x in s.split('/') if x != '']";
            string sExp =
                @"(from x in s.split(""/"")
    where x != """"
    select Convert.ToInt32(x)).ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExListInitializer_SingleItem()
        {
            string pySrc = "['indices'] ";
            string sExp =
                @"new List<object> {
    ""indices""
}";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExNotIn()
        {
            string pySrc = "f not in [a, b]";
            string sExp =
                @"!new List<object> {
    a,
    b
}.Contains(f)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExPow()
        {
            string pySrc = "a  ** b ** c";
            string sExp = "Math.Pow(a, Math.Pow(b, c))";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExRealLiteral()
        {
            string pySrc = "0.1";
            string sExp = "0.1";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExSelf()
        {
            Assert.Equal("this", Xlat("self"));
        }

        [Fact]
        public void ExStringFormat()
        {
            string pySrc = "\"Hello %s%s\" % (world, '!')";
            string sExp = "String.Format(\"Hello %s%s\", world, \"!\")";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExStringLiteral_Quotes()
        {
            string pySrc = "'\"'";
            string sExp = "\"\\\"\"";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(Skip = "Need type inference for this to work correctly")]
        public void ExSubscript()
        {
            string pySrc = "x[2:]";
            string sExp = "x.Skip(2)";
            Assert.Equal(sExp, Xlat(pySrc));
        }
    }
}