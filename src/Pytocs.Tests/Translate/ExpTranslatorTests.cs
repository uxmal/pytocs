#region License
//  Copyright 2015-2022 John Källén
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

using Xunit;
using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.Translate;

namespace Pytocs.UnitTests.Translate
{
    public class ExpTranslatorTests
    {
        private readonly string nl = Environment.NewLine;

        string Xlat(string pyExp)
        {
            var rdr = new StringReader(pyExp);
            var lex = new Lexer("foo.py", rdr);
            var par = new Parser("foo.py", lex);
            var exp = par.test()!;
            Debug.Print("{0}", exp);
            var sym = new SymbolGenerator();
            var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            var xlt = new ExpTranslator(null, types, new CodeGenerator(new CodeCompileUnit(), "", "test"), sym);
            var csExp = exp.Accept(xlt);
            var pvd = new CSharpCodeProvider();
            var writer = new StringWriter();
            pvd.GenerateCodeFromExpression(csExp, writer,
                new CodeGeneratorOptions
                {
                });
            return writer.ToString();
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
        public void ExIsNone()
        {
            Assert.Equal("a is null", Xlat("a is None"));
        }

        [Fact]
        public void ExIsNotNone()
        {
            Assert.Equal("a is not null", Xlat("a is not None"));
        }

        [Fact]
        public void ExIsOneOfManyTypes()
        {
            var pysrc = "isinstance(read_addr, (int, long))";
            var sExp = "read_addr is int || read_addr is long";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void ExSelf()
        {
            Assert.Equal("this", Xlat("self"));
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
        public void ExListComprehension()
        {
            string pySrc = "[int(x) for x in s]";
            string sExp = 
@"(from x in s
    select Convert.ToInt32(x)).ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(Skip = "Need type inference for this to work correctly")]
        public void ExSubscript()
        {
            string pySrc = "x[2:]";
            string sExp = "x.Skip(2)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(ExNegativeSubscript))]
        public void ExNegativeSubscript()
        {
            Assert.Equal("x[^1]", Xlat("x[-1]"));
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
        public void ExAnd()
        {
            string pySrc = "x & 3 and y & 40";
            string sExp = "x & 3 && y & 40";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExStringFormat()
        {
            string pySrc = "\"Hello %s%s\" % (world, '!')";
            string sExp = "String.Format(\"Hello %s%s\", world, \"!\")";
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
        public void ExListInitializer_SingleItem()
        {
            var pySrc = "['indices'] ";
            string sExp =
@"new List<object> {
    ""indices""
}";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void ExStringLiteral_Quotes()
        {
            var pySrc = "'\"'";
            string sExp = "\"\\\"\"";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_isinstance()
        {
            var pySrc = "isinstance(term, Foo)\r\n";
            string sExp = "term is Foo";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_not_isinstance()
        {
            var pySrc = "not isinstance(term, Foo)\r\n";
            string sExp = "!(term is Foo)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_ListFor()
        {
            var pySrc = "[int2byte(b) for b in bytelist]";
            string sExp =
@"(from b in bytelist
    select int2byte(b)).ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_TestExpression()
        {
            var pySrc = "x if cond else y";
            string sExp = "cond ? x : y";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_CompFor))]
        public void Ex_CompFor()
        {
            var pySrc = "sum(int2byte(b) for b in bytelist)";
            string sExp = 
@"(from b in bytelist
    select int2byte(b)).Sum()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_List()
        {
            var pySrc = "list(foo)";
            string sExp = "foo.ToList()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_Regression1))]
        public void Ex_Regression1()
        {
            var pySrc = "((a, s) for a in stackframe.alocs.values() for s in a._segment_list)";
            string sExp =
@"from a in stackframe.alocs.values()
    from s in a._segment_list
    select (a, s)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
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
            Assert.Equal(sExp, Xlat(pySrc));

        }

        [Fact]
        public void Ex_Regression4()
        {
            var pySrc = "{ k:_raw_ast(a[k]) for k in a }";
            var sExp = "a.ToDictionary(k => k, k => _raw_ast(a[k]))";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_SetComprehension()
        {
            var pySrc = "{ id(e) for e in self._breakpoints[t] }";
            var sExp = 
@"(from e in this._breakpoints[t]
    select id(e)).ToHashSet()";
            Assert.Equal(sExp, Xlat(pySrc));
        }


        [Fact]
        public void Ex_SetComprehension2()
        {
            var pySrc = "{(a.addr,b.addr) for a,b in fdiff.block_matches}";
            var sExp =
@"(from _tup_1 in fdiff.block_matches
    let a = _tup_1.Item1
    let b = _tup_1.Item2
    select (a.addr, b.addr)).ToHashSet()";

            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_DictComprehension1()
        {
            var pySrc = "{ k:copy.copy(v) for k, v in path.info.iteritems() }";
            string sExp = "path.info" +
                ".ToDictionary(_tup_1 => _tup_1.Item1, _tup_1 => copy.copy(_tup_1.Item2))";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_DictComprehension2()
        {
            var pySrc = "{ k:k + 'X' for k in path.info }";
            string sExp = "path.info.ToDictionary(k => k, k => k + \"X\")";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_DictComprehension3()
        {
            var pySrc = "{ a + b: b + c for a, b, c in path }";
            string sExp = "path" +
                ".ToDictionary(_tup_1 => _tup_1.Item1 + _tup_1.Item2, _tup_1 => _tup_1.Item2 + _tup_1.Item3)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_NamedParameterCall()
        {
            var pySrc = "foo(bar='baz')";
            var sExp = "foo(bar: \"baz\")";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_HashSet()
        {
            var pySrc = "set()";
            var sExp = "new HashSet<object>()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Theory]
        [InlineData("range(10)", "Enumerable.Range(0, 10)")]
        [InlineData("range(10 + a)", "Enumerable.Range(0, 10 + a)")]
        [InlineData("range(2, 10)", "Enumerable.Range(2, 10 - 2)")]
        [InlineData("range(2 + x, 10)", "Enumerable.Range(2 + x, 10 - (2 + x))")]
        [InlineData("range(2, 10 + y)", "Enumerable.Range(2, 10 + y - 2)")]
        [InlineData("range(2, 10, 2)", "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 - 2) / 2))).Select(_x_1 => 2 + _x_1 * 2)")]
        [InlineData("range(2, 10, 3 + a);", "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 - 2) / (3 + a)))).Select(_x_1 => 2 + _x_1 * (3 + a))")]
        [InlineData("range(2, 10 + b, 5);", "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 + b - 2) / 5))).Select(_x_1 => 2 + _x_1 * 5)")]
        [InlineData("range(2 + c, 10, 20);", "Enumerable.Range(0, Convert.ToInt32(Math.Ceiling(Convert.ToDouble(10 - (2 + c)) / 20))).Select(_x_1 => 2 + c + _x_1 * 20)")]
        public void Ex_range(string pyStm, string sExp)
        {
            var x = Xlat(pyStm);
            Assert.Equal(sExp, x);
        }

        [Fact]
        public void Ex_HashTable()
        {
            var pySrc = "{}";
            var sExp = "new Dictionary<object, object> {" + nl + "}";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Regression5()
        {
            var pySrc = "round(float( float(count) / float(self.insn_count)), 3) >= .67";
            var sExp = "round(Convert.ToDouble(Convert.ToDouble(count) / Convert.ToDouble(this.insn_count)), 3) >= 0.67";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_ByteConstant()
        {
            var pySrc = "b'\\xfe\\xed' * init_stack_size";
            var sExp = "new byte[] { 0xfe, 0xed } * init_stack_size";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_intrinsic_len()
        {
            var pysrc = "len(foo.bar)";
            var sExp = "foo.bar.Count";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_iteritems()
        {
            var pysrc = "foo.iteritems()";
            var sExp = "foo";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_itervalues()
        {
            var pysrc = "foo.itervalues()";
            var sExp = "foo.Values";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact(DisplayName = nameof(Ex_instrinsic_iterkeys))]
        public void Ex_instrinsic_iterkeys()
        {
            var pysrc = "foo.iterkeys()";
            var sExp = "foo.Keys";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_sum()
        {
            var pysrc = "sum(bar)";
            var sExp = "bar.Sum()";
            Assert.Equal(sExp, Xlat(pysrc));
        }
        
        [Fact]
        public void Ex_instrinsic_list()
        {
            var pysrc = "list()";
            var sExp = "new List<object>()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_set()
        {
            var pysrc = "set()";
            var sExp = "new HashSet<object>()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_instrinsic_set_with_args()
        {
            var pysrc = "set(a)";
            var sExp = "new HashSet<object>(a)";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_dict()
        {
            var pysrc = "dict()";
            var sExp = "new Dictionary<object, object>()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
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
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_dict_iterable()
        {
            var pysrc = "dict(a)";
            var sExp = "a.ToDictionary()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_filter()
        {
            var pysrc = "filter(fn, items)";
            var sExp = "items.Where(fn).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_filter_none()
        {
            var pysrc = "filter(None, items)";
            var sExp = "items.Where(_p_1 => _p_1 != null).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted()
        {
            var pysrc = "sorted(items)";
            var sExp = "items.OrderBy(_p_1 => _p_1).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted_cmp()
        {
            var pysrc = "sorted(items, cmp)";
            var sExp = "items.OrderBy(cmp).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted_key()
        {
            var pysrc = "sorted(items, key=lambda x: x.addr)";
            var sExp = "items.OrderBy(x => x.addr).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_sorted_reverse_bool()
        {
            var pysrc = "sorted(items, reverse=true)";
            var sExp = "items.OrderBy(_p_1 => _p_1).ToList()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_intrinsic_enumerate()
        {
            var pysrc = "enumerate(items)";
            var sExp = "items.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_for_with_if()
        {
            var pysrc = "(a for a in function.endpoints if a.addr == endpoint_addr)";
            var sExp =
@"from a in function.endpoints
    where a.addr == endpoint_addr
    select a";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_for_with_if_projected()
        {
            var pysrc = "(a.x for a in function.endpoints if a.addr == endpoint_addr)";
            var sExp =
@"from a in function.endpoints
    where a.addr == endpoint_addr
    select a.x";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_for_with_if_projected_to_set()
        {
            var pysrc = "{a for a in function.endpoints if a.addr == endpoint_addr}";
            var sExp =
@"(from a in function.endpoints
    where a.addr == endpoint_addr
    select a).ToHashSet()";
            Assert.Equal(sExp, Xlat(pysrc));
        }

        [Fact]
        public void Ex_dict_Comprehension()
        {
            var pySrc = "{AT.from_rva(v, self).to_mva(): k for (k, v) in self._plt.iteritems()}";
            var sExp = "this._plt.ToDictionary(_de1 => AT.from_rva(_de1.Value, this).to_mva(), _de1 => _de1.Key)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_struct_unpack()
        {
            var pySrc = "struct.unpack('3x', buffer)";
            var sExp = "@struct.unpack(\"3x\", buffer)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_ScientificNotation()
        {
            var pySrc = "1E-5";
            var sExp = "1E-05";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_SmallInteger()
        {
            var pySrc = "128";
            var sExp = "128";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_Github_Issue_14()
        {
            var pySrc = "len(x + y + z)";
            var sExp = "(x + y + z).Count";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        // Reported in Github issue #17
        [Fact]
        public void Ex_Associativity()
        {
            string pySrc = "a - (b + c)";
            string sExp = "a - (b + c)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_NestedForIfComprehensions))]
        public void Ex_NestedForIfComprehensions()
        {
            var pySrc = "[state for (stash, states) in self.simgr.stashes.items() if (stash != 'pruned') for state in states ]";
            var sExp =
@"(from _tup_1 in this.simgr.stashes.items()
    let stash = _tup_1.Item1
    let states = _tup_1.Item2
    where stash != ""pruned""
    from state in states
    select state).ToList()";
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
        public void Ex_Complex_Literal()
        {
            string pySrc = "3 - 4j";
            string sExp = "new Complex(3.0, -4.0)";
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
        public void Ex_Await()
        {
            string pySrc = "await foo";
            string sExp = "await foo";
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

        [Fact(DisplayName = nameof(Ex_ListComprehension_Alternating_fors))]
        public void Ex_ListComprehension_Alternating_fors()
        {
            var pySrc = "[state for (stash, states) in self.simgr.stashes.items() if stash != 'pruned' for state in states]";
            var sExp =
@"(from _tup_1 in this.simgr.stashes.items()
    let stash = _tup_1.Item1
    let states = _tup_1.Item2
    where stash != ""pruned""
    from state in states
    select state).ToList()";

            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_nested_for_comprehension))]
        public void Ex_nested_for_comprehension()
        {
            var pySrc = "((a, b) for a,b in list for a,b in (a,b)))";
            var sExp =
@"from _tup_1 in list
    let a = _tup_1.Item1
    let b = _tup_1.Item2
    from _tup_2 in (a, b)
    let a = _tup_2.Item1
    let b = _tup_2.Item2
    select (a, b)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_str_builtin))]
        public void Ex_str_builtin()
        {
            var pySrc = "str(a)";
            var sExp = "a.ToString()";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_str_builtin_encoding))]
        public void Ex_str_builtin_encoding()
        {
            var pySrc = "str(a, 'utf-8')";
            var sExp = "Encoding.GetEncoding(\"utf-8\").GetString(a)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_str_parameterless))]
        public void Ex_str_parameterless()
        {
            var pySrc = "str()";
            var sExp = @"String.Empty";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_issue_57))]
        public void Ex_issue_57()
        {
            var pySrc = "{'a': 'str', **kwargs }";
            var sExp = @"DictionaryUtils.Unpack<string, object>((""a"", ""str""), kwargs)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_lambda_kwargs()
        {
            var pySrc = "lambda x, **k: xpath_text(hd_doc, './/video/' + x, **k)";
            var sExp = "(x,k) => xpath_text(hd_doc, \".//video/\" + x, k)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_set_unpackers()
        {
            var pySrc = "{ *set1, 0, 1, *set2, '3' }";
            var sExp = "SetUtils.Unpack<object>(set1, new object[] {" + nl +
                "    0," + nl +
                "    1," + nl +
                "}, set2, new object[] {" + nl +
                "    \"3\"," + nl +
                "})";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_hex_literal()
        {
            var pySrc = "0x_1234";
            var sExp = "0x_1234";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact]
        public void Ex_list_unpackers()
        {
            var pySrc = "[ 1, 2, *foo, 3, 4]";
            var sExp =
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
        public void Ex_tuple_unpacker()
        {
            var pySrc = "(1, 2, *foo, 3, 4)";
            var sExp =
@"TupleUtils.Unpack<object>(new object[] {
    1,
    2,
}, foo, new object[] {
    3,
    4,
})";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_FormatString))]
        public void Ex_FormatString()
        {
            var pySrc = "f'Hello {world}'";
            var sExp = "$\"Hello {world}\"";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_DictComprehension))]
        public void Ex_DictComprehension()
        {
            var pySrc = "{va: size for (va, size, fva) in vw.getFunctionBlocks(funcva)}";
            var sExp =
@"(from _tup_1 in vw.getFunctionBlocks(funcva)
    let va = _tup_1.Item1
    let size = _tup_1.Item2
    let fva = _tup_1.Item3
    select (va, size)).ToDictionary(_de_1 => _de_1.Item1, _de_1 => _de_1.Item2)";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_SetInitializer))]
        public void Ex_SetInitializer()
        {
            var pySrc = "{ a, b, c }";
            var sExp = 
@"new HashSet {
    a,
    b,
    c
}";
            Assert.Equal(sExp, Xlat(pySrc));
        }

        [Fact(DisplayName = nameof(Ex_MatrixMultiplication))]
        public void Ex_MatrixMultiplication()
        {
            var pySrc = "a @ b";
            var sExp = "a.@__matmul__(b)";
            Assert.Equal(sExp, Xlat(pySrc));
        }
    }
}
