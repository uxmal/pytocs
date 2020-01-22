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

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Pytocs.Core.Syntax
{
    public abstract class Exp : Node
    {
        public Exp(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public string Comment { get; set; }

        public abstract T Accept<T>(IExpVisitor<T> v);

        public abstract void Accept(IExpVisitor v);

        public sealed override string ToString()
        {
            StringWriter sw = new StringWriter();
            Write(sw);
            return sw.ToString();
        }

        public virtual void Write(TextWriter writer)
        {
            writer.Write(GetType().FullName);
        }

        internal string OpToString(Op op)
        {
            switch (op)
            {
                default: throw new NotSupportedException(string.Format("Unknown op {0}.", op));
                case Op.Ge: return " >= ";
                case Op.Le: return " <= ";
                case Op.Lt: return " < ";
                case Op.Gt: return " > ";
                case Op.Eq: return "=";
                case Op.Ne: return " != ";
                case Op.In: return "in";
                case Op.NotIn: return "not in";
                case Op.Is: return "is";
                case Op.IsNot: return "is not";
                case Op.Xor: return "^";
                case Op.LogOr: return "or";
                case Op.LogAnd: return "and";
                case Op.Shl: return " << ";
                case Op.Shr: return " >> ";
                case Op.Add: return " + ";
                case Op.Sub: return " - ";
                case Op.Mul: return " * ";
                case Op.Div: return " /";
                case Op.IDiv: return " // ";
                case Op.Mod: return "%";
                case Op.Complement: return "~";
                case Op.AugAdd: return " += ";
                case Op.AugSub: return " -= ";
                case Op.AugMul: return " *= ";
                case Op.AugDiv: return " /= ";
                case Op.AugMod: return " %= ";
                case Op.AugAnd: return " &= ";
                case Op.AugOr: return " |= ";
                case Op.AugXor: return " ^= ";
                case Op.AugShl: return " <<= ";
                case Op.AugShr: return " >>= ";
                case Op.AugExp: return " **= ";
                case Op.AugIDiv: return " //= ";
                case Op.BitAnd: return "&";
                case Op.BitOr: return "|";
                case Op.Not: return "not ";
                case Op.Exp: return " ** ";
                case Op.Assign: return "=";
            }
        }

        public virtual IEnumerable<Exp> AsList()
        {
            yield return this;
        }
    }

    public class NoneExp : Exp
    {
        public NoneExp(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitNoneExp();
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitNoneExp();
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("None");
        }
    }

    public class BooleanLiteral : Exp
    {
        public readonly bool Value;

        public BooleanLiteral(bool b, string filename, int start, int end) : base(filename, start, end)
        {
            Value = b;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBooleanLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBooleanLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Value ? "True" : "False");
        }
    }

    public class Bytes : Exp
    {
        public readonly string s;
        public bool Raw;

        public Bytes(string str, string filename, int start, int end)
            : base(filename, start, end)
        {
            s = str;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBytes(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBytes(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("b");
            if (Raw)
            {
                writer.Write("r");
            }

            writer.Write('\"');
            writer.Write(s);
            writer.Write('\"');
        }
    }

    /// <summary>
    /// Python string literal.
    /// </summary>
    public class Str : Exp
    {
        public readonly string s;
        public bool Format; // true if this is a format string.
        public bool Long;
        public bool Raw;
        public bool Unicode;

        public Str(string str, string filename, int start, int end) : base(filename, start, end)
        {
            s = str;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitStr(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitStr(this);
        }

        public override void Write(TextWriter writer)
        {
            if (Raw)
            {
                writer.Write("r");
            }

            writer.Write('\"');
            writer.Write(s);
            writer.Write('\"');
        }
    }

    public class IntLiteral : Exp
    {
        public readonly long NumericValue;
        public readonly string Value;

        public IntLiteral(string value, long p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitIntLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitIntLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Value);
        }
    }

    public class LongLiteral : Exp
    {
        public readonly long NumericValue;
        public readonly string Value;

        public LongLiteral(string value, long p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitLongLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitLongLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{0}L", Value);
        }
    }

    public class BigLiteral : Exp
    {
        public BigLiteral(string value, BigInteger p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public string Value { get; }
        public BigInteger NumericValue { get; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBigLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBigLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{0}", Value);
        }
    }

    public class RealLiteral : Exp
    {
        public readonly double NumericValue;

        public readonly string Value;

        public RealLiteral(string value, double p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitRealLiteral(this);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitRealLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            if (NumericValue == double.PositiveInfinity)
            {
                writer.Write("float('+inf')");
            }
            else if (NumericValue == double.NegativeInfinity)
            {
                writer.Write("float('-inf')");
            }
            else
            {
                writer.Write(Value);
            }
        }
    }

    public class ImaginaryLiteral : Exp
    {
        public ImaginaryLiteral(string value, double im, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = im;
        }

        public string Value { get; }
        public double NumericValue { get; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitImaginary(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitImaginaryLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Value);
            writer.Write("j");
        }
    }

    public class BinExp : Exp
    {
        public Exp l;
        public Op op;
        public Exp r;

        public BinExp(Op op, Exp l, Exp r, string filename, int start, int end) : base(filename, start, end)
        {
            this.op = op; this.l = l; this.r = r;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBinExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBinExp(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("(");
            l.Write(writer);
            writer.Write(" {0} ", OpToString(op));
            r.Write(writer);
            writer.Write(")");
        }
    }

    public class DictComprehension : Exp
    {
        public Exp key;
        public CompFor source;
        public Exp value;

        public DictComprehension(Exp key, Exp value, CompFor collection, string filename, int start, int end) : base(filename, start, end)
        {
            this.key = key;
            this.value = value;
            source = collection;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitDictComprehension(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitDictComprehension(this);
        }
    }

    public class DictInitializer : Exp
    {
        public DictInitializer(List<KeyValuePair<Exp, Exp>> keyValues, string filename, int start, int end)
            : base(filename, start, end)
        {
            KeyValues = keyValues;
        }

        public List<KeyValuePair<Exp, Exp>> KeyValues { get; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitDictInitializer(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitDictInitializer(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{");
            writer.Write(" ");
            foreach (KeyValuePair<Exp, Exp> kv in KeyValues)
            {
                if (kv.Key != null)
                {
                    kv.Key.Write(writer);
                    writer.Write(" : ");
                    kv.Value.Write(writer);
                }
                else
                {
                    writer.Write("**");
                    kv.Value.Write(writer);
                }
                writer.Write(", ");
            }
            writer.Write(" ");
            writer.Write("}");
        }
    }

    public class IterableUnpacker : Exp
    {
        public readonly Exp Iterable;

        public IterableUnpacker(Exp iterable, string filename, int start, int end) : base(filename, start, end)
        {
            Iterable = iterable;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitIterableUnpacker(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitIterableUnpacker(this);
        }

        public override void Write(TextWriter w)
        {
            w.Write("*");
            Iterable.Write(w);
        }
    }

    public class Ellipsis : Exp
    {
        public Ellipsis(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitEllipsis(this);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitEllipsis(this);
        }
    }

    public class UnaryExp : Exp
    {
        public Exp e;       //$TODO: rename to Expression.
        public Op op;

        public UnaryExp(Op op, Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            this.op = op;
            e = exp;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitUnary(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitUnary(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(OpToString(op));
            e.Write(writer);
        }
    }

    public class TestExp : Exp
    {
        public Exp Alternative;
        public Exp Condition;
        public Exp Consequent;

        public TestExp(string filename, int start, int end)
            : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitTest(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitTest(this);
        }

        public override void Write(TextWriter writer)
        {
            Consequent.Write(writer);
            writer.Write(" ");
            writer.Write("if");
            writer.Write(" ");
            Condition.Write(writer);
            writer.Write(" ");
            writer.Write("else");
            writer.Write(" ");
            Alternative.Write(writer);
        }
    }

    public class Identifier : Exp
    {
        public Identifier(string name, string filename, int start, int end) : base(filename, start, end)
        {
            Name = name;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitIdentifier(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitIdentifier(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Name);
        }
    }

    public class Application : Exp
    {
        public readonly List<Argument> args;
        public readonly Exp fn;
        public readonly List<Argument> keywords;
        public readonly Exp kwargs;
        public readonly Exp stargs;

        public Application(Exp fn, List<Argument> args, List<Argument> keywords, Exp stargs, Exp kwargs,
            string filename, int start, int end) : base(filename, start, end)
        {
            this.fn = fn;
            this.args = args;
            this.keywords = keywords;
            this.stargs = stargs;
            this.kwargs = kwargs;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitApplication(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitApplication(this);
        }

        public override void Write(TextWriter writer)
        {
            fn.Write(writer);
            writer.Write("(");
            string sep = "";
            foreach (Argument arg in args)
            {
                writer.Write(sep);
                arg.Write(writer);
                sep = ",";
            }
            foreach (Argument arg in keywords)
            {
                writer.Write(sep);
                arg.Write(writer);
                sep = ",";
            }
            if (stargs != null)
            {
                writer.Write(sep);
                writer.Write("*");
                stargs.Write(writer);
                sep = ",";
            }
            if (kwargs != null)
            {
                writer.Write(sep);
                writer.Write("**");
                kwargs.Write(writer);
            }
            writer.Write(")");
        }
    }

    public class ArrayRef : Exp
    {
        public readonly Exp array;
        public readonly List<Slice> subs;

        public ArrayRef(Exp array, List<Slice> subs, string filename, int start, int end) : base(filename, start, end)
        {
            this.array = array;
            this.subs = subs;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitArrayRef(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitArrayRef(this);
        }

        public override void Write(TextWriter writer)
        {
            array.Write(writer);
            writer.Write("[");
            foreach (Slice slice in subs)
            {
                slice.Write(writer);
            }

            writer.Write("]");
        }
    }

    public class Slice : Exp
    {
        public Exp lower;
        public Exp step;
        public Exp upper;

        public Slice(Exp start, Exp end, Exp slice, string filename, int s, int e) : base(filename, s, e)
        {
            lower = start;
            step = end;
            upper = slice;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitSlice(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitSlice(this);
        }

        public override void Write(TextWriter writer)
        {
            if (lower == null && step == null && upper == null)
            {
                writer.Write("::");
            }
            else if (lower != null)
            {
                lower.Write(writer);
                if (step != null)
                {
                    writer.Write(':');
                    step.Write(writer);
                    writer.Write(':');
                    if (upper != null)
                    {
                        writer.Write(':');
                        upper.Write(writer);
                    }
                }
            }
        }
    }

    public class AttributeAccess : Exp
    {
        public readonly Exp Expression;
        public readonly Identifier FieldName;

        public AttributeAccess(Exp expr, Identifier fieldName, string filename, int start, int end) : base(filename, start, end)
        {
            Expression = expr;
            FieldName = fieldName;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitFieldAccess(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitFieldAccess(this);
        }

        public override void Write(TextWriter w)
        {
            Expression.Write(w);
            w.Write(".{0}", FieldName);
        }
    }

    public class AwaitExp : Exp
    {
        public readonly Exp exp;

        public AwaitExp(Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            this.exp = exp;
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitAwait(this);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitAwait(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("await");
            writer.Write(" ");
            exp.Write(writer);
        }
    }

    public class YieldExp : Exp
    {
        public readonly Exp exp;

        public YieldExp(Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            this.exp = exp;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitYieldExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitYieldExp(this);
        }

        public override void Write(TextWriter writer)
        {
            exp.Write(writer);
        }
    }

    public class YieldFromExp : Exp
    {
        public readonly Exp Expression;

        public YieldFromExp(Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            Expression = exp;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitYieldFromExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitYieldFromExp(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("from");
            writer.Write(" ");
            Expression.Write(writer);
        }
    }

    public abstract class CompIter : Exp
    {
        public CompIter next;

        public CompIter(string filename, int start, int end) : base(filename, start, end)
        {
        }
    }

    public class CompFor : CompIter
    {
        public Exp collection;
        public Exp variable;

        public CompFor(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public bool Async { get; set; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitCompFor(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitCompFor(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("for");
            writer.Write(" ");
            variable.Write(writer);
            writer.Write(" ");
            writer.Write("in");
            writer.Write(" ");
            collection.Write(writer);
            if (next != null)
            {
                writer.Write(" ");
                next.Write(writer);
            }
        }
    }

    public class CompIf : CompIter
    {
        public Exp test;

        public CompIf(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitCompIf(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitCompIf(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("if");
            writer.Write(" ");
            test.Write(writer);
            if (next != null)
            {
                writer.Write(" ");
                next.Write(writer);
            }
        }
    }

    public class PySet : Exp
    {
        public List<Exp> exps;

        public PySet(List<Exp> exps, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.exps = exps;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitSet(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitSet(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{ ");
            string sep = "";
            foreach (Exp item in exps)
            {
                writer.Write(sep);
                sep = ", ";
                item.Write(writer);
            }
            writer.Write(" }");
        }
    }

    public class StarExp : Exp
    {
        public Exp e;

        public StarExp(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitStarExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitStarExp(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("*");
            e.Write(writer);
        }
    }

    public class ExpList : Exp
    {
        public readonly List<Exp> Expressions;

        public ExpList(List<Exp> exps, string filename, int start, int end) : base(filename, start, end)
        {
            Expressions = exps;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitExpList(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitExpList(this);
        }

        public override IEnumerable<Exp> AsList()
        {
            return Expressions;
        }

        public override void Write(TextWriter writer)
        {
            string sep = "";
            foreach (Exp exp in Expressions)
            {
                writer.Write(sep);
                sep = ",";
                exp.Write(writer);
            }
        }
    }

    public class PyList : Exp
    {
        public readonly List<Exp> elts;

        public PyList(List<Exp> elts, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.elts = elts;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitList(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitList(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("[");
            string sep = "";
            foreach (Exp exp in elts)
            {
                writer.Write(sep);
                sep = ",";
                exp.Write(writer);
            }
            writer.Write("]");
        }
    }

    public class GeneratorExp : Exp
    {
        public Exp Collection;
        public Exp Projection;

        public GeneratorExp(Exp proj, Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            Projection = proj;
            Collection = coll;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitGeneratorExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitGeneratorExp(this);
        }
    }

    public class ListComprehension : Exp
    {
        public Exp Collection;
        public Exp Projection;

        public ListComprehension(Exp proj, Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            Projection = proj;
            Collection = coll;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitListComprehension(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitListComprehension(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("[");
            Projection.Write(writer);
            writer.Write(" ");
            Collection.Write(writer);
            writer.Write("]");
        }
    }

    public class SetComprehension : Exp
    {
        public Exp Collection;
        public Exp Projection;

        public SetComprehension(Exp proj, Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            Projection = proj;
            Collection = coll;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitSetComprehension(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitSetComprehension(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{");
            Projection.Write(writer);
            writer.Write(" ");
            Collection.Write(writer);
            writer.Write("}");
        }
    }

    public class AssignExp : Exp
    {
        public readonly Exp Annotation;
        public readonly Exp Dst;
        public readonly Op op;
        public readonly Exp Src;

        public AssignExp(Exp lhs, Op op, Exp rhs, string filename, int start, int end)
            : base(filename, start, end)
        {
            Dst = lhs;
            this.op = op;
            Src = rhs;
            Annotation = null;
        }

        public AssignExp(Exp lhs, Exp annotation, Op op, Exp rhs, string filename, int start, int end)
        : base(filename, start, end)
        {
            Dst = lhs;
            this.op = op;
            Src = rhs;
            Annotation = annotation;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitAssignExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitAssignExp(this);
        }

        public override void Write(TextWriter writer)
        {
            Dst.Write(writer);
            if (Annotation != null)
            {
                writer.Write(": ");
                Annotation.Write(writer);
            }
            writer.Write(OpToString(op));
            Src.Write(writer);
        }
    }

    public class PyTuple : Exp
    {
        public List<Exp> values;

        public PyTuple(List<Exp> values, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.values = values;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitTuple(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitTuple(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("(");
            string sep = "";
            foreach (Exp e in values)
            {
                writer.Write(sep);
                sep = ",";
                e.Write(writer);
            }
            writer.Write(")");
        }
    }

    ///
    /// virtual-AST node used to represent virtual source locations for builtins
    /// as external urls.
    ///
    public class Url : Exp
    {
        public string url;

        public Url(string url) : base(null, -1, -1)
        {
            this.url = url;
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("<Url:\"{0}\">", url);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            throw new NotImplementedException();
        }

        public override void Accept(IExpVisitor v)
        {
            throw new NotImplementedException();
        }
    }
}