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

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Globalization;

namespace Pytocs.Core.Syntax
{
    public abstract class Exp : Node
    {
        public Exp(string filename, int start, int end) : base(filename, start, end) 
        {
        }

        public string? Comment { get; set; }

        public abstract T Accept<T>(IExpVisitor<T> v);

        public abstract T Accept<T,C>(IExpVisitor<T, C> v, C context);

        public abstract void Accept(IExpVisitor v);

        public sealed override string ToString()
        {
            var sw = new StringWriter();
            Write(sw);
            return sw.ToString();
        }

        public virtual void Write(TextWriter writer)
        {
            writer.Write(GetType().FullName);
        }

        public string OpToString(Op op)
        {
            return op switch
            {
                Op.Ge => " >= ",
                Op.Le => " <= ",
                Op.Lt => " < ",
                Op.Gt => " > ",
                Op.Eq => "=",
                Op.Ne => " != ",
                Op.In => "in",
                Op.NotIn => "not in",
                Op.Is => "is",
                Op.IsNot => "is not",
                Op.Xor => "^",
                Op.LogOr => "or",
                Op.LogAnd => "and",
                Op.Shl => " << ",
                Op.Shr => " >> ",
                Op.Add => " + ",
                Op.Sub => " - ",
                Op.Mul => " * ",
                Op.Div => " /",
                Op.IDiv => " // ",
                Op.Mod => "%",
                Op.MatMul => "@",
                Op.Complement => "~",
                Op.AugAdd => " += ",
                Op.AugSub => " -= ",
                Op.AugMul => " *= ",
                Op.AugDiv => " /= ",
                Op.AugMod => " %= ",
                Op.AugAnd => " &= ",
                Op.AugOr => " |= ",
                Op.AugXor => " ^= ",
                Op.AugShl => " <<= ",
                Op.AugShr => " >>= ",
                Op.AugExp => " **= ",
                Op.AugIDiv => " //= ",
                Op.AugMatMul => " @= ",
                Op.BitAnd => "&",
                Op.BitOr => "|",
                Op.Not => "not ",
                Op.Exp => " ** ",
                Op.Assign => "=",
                _ => throw new NotSupportedException(string.Format("Unknown op {0}.", op))
            };
            }

        public virtual IEnumerable<Exp> AsList()
        {
            yield return this;
        }
    }

    public class NoneExp : Exp
    {
        public NoneExp(string filename, int start, int end) 
            : base(filename, start, end) { }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitNoneExp(context);
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
        public BooleanLiteral(bool b, string filename, int start, int end) : base(filename, start, end) 
        {
            Value = b;
        }

        public bool Value { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitBooleanLiteral(this, context);
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
            this.s = str;
        }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitBytes(this, context);
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
                writer.Write("r");
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
        public bool Raw;
        public bool Unicode;
        public bool Long;
        public bool Format; // true if this is a format string.

        public Str(string str, string filename, int start, int end) : base(filename, start, end) 
        {
            this.Value = str;
        }

        public string Value { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitStr(this, context);
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
                writer.Write("r");
            writer.Write('\"');
            writer.Write(Value);
            writer.Write('\"');
        }
        }

    public class IntLiteral : Exp
    {
        public IntLiteral(string value, long p, string filename, int start, int end) : base(filename, start, end) 
        {
            this.Value = value;
            this.NumericValue = p;
        }

        public long NumericValue { get; }

        public string Value { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitIntLiteral(this, context);
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
        public LongLiteral(string value, long p, string filename, int start, int end) : base(filename, start, end)
        {
            this.Value = value;
            this.NumericValue = p;
        }

        public long NumericValue { get; }

        public string Value { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitLongLiteral(this, context);
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
            this.Value = value;
            this.NumericValue = p;
        }

        public BigInteger NumericValue { get; }

        public string Value { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitBigLiteral(this, context);
        }

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
        public RealLiteral(string value, double p, string filename, int start, int end) : base(filename, start, end)
        {
            this.Value = value;
            this.NumericValue = p;
        }

        public double NumericValue { get; }

        public string Value { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitRealLiteral(this, context);
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
                writer.Write("float('+inf')");
            else if (NumericValue == double.NegativeInfinity)
                writer.Write("float('-inf')");
            else
            {
                writer.Write(Value.ToString(CultureInfo.InvariantCulture));
    }
        }
    }

    public class ImaginaryLiteral : Exp
    {
        public ImaginaryLiteral(string value, double im, string filename, int start, int end) : base(filename, start, end)
        {
            this.Value = value;
            this.NumericValue = im;
        }

        public string Value { get; }
        public double NumericValue { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitImaginary(this, context);
        }

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
        public BinExp(Op op, Exp l, Exp r, string filename, int start, int end) : base(filename, start, end)
        {
            this.Operator = op;
            this.Left = l;
            this.Right = r;
        }

        public Op Operator { get; }
        public Exp Left { get; }
        public Exp Right { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitBinExp(this, context);
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
            Left.Write(writer);
            writer.Write(" {0} ", base.OpToString(Operator));
            Right.Write(writer);
            writer.Write(")");
        }
    }

    public class DictComprehension : Exp
    {
        public DictComprehension(Exp key, Exp value, CompFor collection,  string filename, int start, int end) 
            : base(filename, start, end) 
        {
            this.Key = key;
            this.Value = value;
            this.Source = collection;
        }

        public Exp Key { get; }
        public Exp Value { get; }
        public CompFor Source { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitDictComprehension(this, context);
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
        public DictInitializer(List<(Exp? Key, Exp Value)> keyValues, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.KeyValues = keyValues;
        }

        public List<(Exp? Key, Exp Value)> KeyValues { get; }


        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitDictInitializer(this, context);
        }

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
            foreach (var kv in KeyValues)
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
        public IterableUnpacker(Exp iterable, string filename, int start, int end) : base(filename, start, end)
        {
            this.Iterable = iterable;
        }

        public Exp Iterable { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitIterableUnpacker(this, context);
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
            this.Iterable.Write(w);
        }
    }

    public class Ellipsis : Exp
    {
        public Ellipsis(string filename, int start, int end) : base(filename, start, end) { }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitEllipsis(this, context);
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
        public UnaryExp(Op op, Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            this.Operator = op;
            this.Exp = exp;
        }

        public Op Operator { get; }
        public Exp Exp { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitUnary(this, context);
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
            writer.Write(OpToString(Operator));
            Exp.Write(writer);
        }
    }

    public class TestExp : Exp
    {
        public TestExp(Exp consequent, Exp condition, Exp alternative, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Consequent = consequent;
            this.Condition = condition;
            this.Alternative = alternative;
        }

        public Exp Condition { get; }
        public Exp Consequent { get; }
        public Exp Alternative { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitTest(this, context);
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
            this.Consequent.Write(writer);
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
            this.Name = name;
        }

        public string Name { get; }
        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitIdentifier(this, context);
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
        public Application(Exp fn, List<Argument> args, List<Argument> keywords, Exp? stargs, Exp? kwargs,
            string filename, int start, int end) : base(filename, start, end)
        {
            this.Function = fn;
            this.Args = args;
            this.Keywords = keywords;
            this.StArgs = stargs;
            this.KwArgs = kwargs;
        }

        public Exp Function { get; }
        public List<Argument> Args { get; }
        public List<Argument> Keywords { get; }
        public Exp? StArgs { get; }
        public Exp? KwArgs { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitApplication(this, context);
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
            Function.Write(writer);
            writer.Write("(");
            var sep = "";
            foreach (var arg in Args)
            {
                writer.Write(sep);
                arg.Write(writer);
                sep = ",";
            }
            foreach (var arg in Keywords)
                {
                    writer.Write(sep);
                    arg.Write(writer);
                    sep = ",";
                }
            if (StArgs != null)
            {
                writer.Write(sep);
                writer.Write("*");
                StArgs.Write(writer);
                sep = ",";
            }
            if (KwArgs != null)
            {
                writer.Write(sep);
                writer.Write("**");
                KwArgs.Write(writer);
            }
            writer.Write(")");
        }
    }

    public class ArrayRef : Exp
    {
        public ArrayRef(Exp array, List<Slice> subs, string filename, int start, int end) : base(filename, start, end)
        {
            this.Array = array;
            this.Subs = subs;
        }

        public Exp Array { get; }
        public List<Slice> Subs { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitArrayRef(this, context);
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
            Array.Write(writer);
            writer.Write("[");
            foreach (var slice in Subs)
            {
                slice.Write(writer);
            }
            writer.Write("]");
        }
    }

    public class Slice : Exp
    {
        public Slice(Exp? start, Exp? end, Exp? stride, string filename, int s, int e) : base(filename, s, e)
        {
            this.Lower = start;
            this.Upper = end;
            this.Stride = stride;
        }

        public Exp? Lower { get; }
        public Exp? Upper { get; }
        public Exp? Stride { get; }


        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitSlice(this, context);
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
            if (Lower == null && Upper == null && Stride == null)
            {
                writer.Write(":");
            }
            else if (Lower != null)
            {
                Lower.Write(writer);
                if (Upper != null)
                {
                    writer.Write(':');
                    Upper.Write(writer);
                    if (Stride != null)
                    {
                        writer.Write(':');
                        Stride.Write(writer);
                    }
                }
            }
        }
    }

    public class AttributeAccess : Exp
    {
        public AttributeAccess(Exp expr, Identifier fieldName, string filename, int start, int end) : base(filename, start, end)
        {
            Expression = expr; 
            FieldName = fieldName; 
        }

        public Exp Expression { get; }
        public Identifier FieldName { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitFieldAccess(this, context);
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
        public AwaitExp(Exp exp, string filename, int start, int end) :base(filename, start, end)
        {
            this.Exp = exp;
        }

        public Exp Exp { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitAwait(this, context);
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
            this.Exp.Write(writer);
        }
    }

    public class YieldExp : Exp
    {
        public YieldExp(Exp? exp, string filename, int start, int end) : base(filename, start, end) { this.Expression = exp; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitYieldExp(this, context);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitYieldExp(this);
        }

        public Exp? Expression { get; }

        public override void Accept(IExpVisitor v)
        {
            v.VisitYieldExp(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("yield");
            if (Expression != null)
            {
                writer.Write(" ");
                Expression.Write(writer);
        }
    }
    }

    public class YieldFromExp : Exp
    {
        public YieldFromExp(Exp exp, string filename, int start, int end) : base(filename, start, end) { this.Expression = exp; }

        public Exp Expression { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitYieldFromExp(this, context);
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
        public CompIter? next;

        public CompIter(string filename, int start, int end) 
            : base(filename, start, end)
        {
        }
    }

    public class CompFor : CompIter
    {
        public Exp variable;
        public Exp collection;
        public Exp? projection;

        public CompFor(Exp? projection, Exp variable, Exp collection, string filename, int start, int end) : base(filename, start, end)
        {
            this.projection = projection;
            this.variable = variable;
            this.collection = collection;
        }

        public bool Async { get; set; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitCompFor(this, context);
        }

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
            if (projection != null)
            {
                projection.Write(writer);
                writer.Write(" ");
            }
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
        public CompIf(Exp test, string filename, int start, int end) : base(filename, start, end)
        {
            this.Test = test;
        }

        public Exp Test { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitCompIf(this, context);
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
            Test.Write(writer);
            if (next != null)
            {
                writer.Write(" ");
                next.Write(writer);
            }
        }
    }

    public class PySet : Exp
    {
        public PySet(List<Exp> exps, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Initializer = exps;
        }

        public List<Exp> Initializer { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitSet(this, context);
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
            var sep = "";
            foreach (var item in Initializer)
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
        public StarExp(Exp e, string filename, int start, int end) : base(filename, start, end)
        {
            this.Expression = e;
        }

        public Exp Expression { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitStarExp(this, context);
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
            this.Expression.Write(writer);
    }
    }

    public class ExpList : Exp
    {
        public ExpList(List<Exp> exps, string filename, int start, int end) : base(filename, start, end)
        {
            this.Expressions = exps;
        }

        public List<Exp> Expressions { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitExpList(this, context);
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
            if (Expressions.Count == 1)
            {
                Expressions[0].Write(writer);
                writer.Write(",");
            }
            else
            {
            var sep = "";
            foreach (var exp in Expressions)
            {
                writer.Write(sep);
                sep = ",";
                exp.Write(writer);
            }
        }
    }
    }

    /// <summary>
    /// A list of expressions.
    /// </summary>
    public class PyList : Exp
    {
        public PyList(List<Exp> elts, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Initializer = elts;
        }

        public List<Exp> Initializer { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitList(this, context);
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
            var sep = "";
            foreach (var exp in Initializer)
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
        public GeneratorExp(Exp proj, Exp coll, string filename, int start, int end) : base(filename,start, end)
        {
            this.Projection = proj;
            this.Collection = coll;
        }

        public Exp Projection { get; }
        public Exp Collection { get; }


        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitGeneratorExp(this, context);
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
        public ListComprehension(Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            this.Collection = coll;
        }

        public Exp Collection { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitListComprehension(this, context);
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
            Collection.Write(writer);
            writer.Write("]");
        }
    }

    public class SetComprehension : Exp
    {
        public SetComprehension(Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            this.Collection = coll;
        }

        public Exp Collection { get; }


        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitSetComprehension(this, context);
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
            Collection.Write(writer);
            writer.Write("}");
        }
    }

    public class AssignExp : Exp
    {
        public AssignExp(Exp lhs, Op op, Exp rhs, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Dst = lhs;
            this.Operator = op;
            this.Src = rhs;
            this.Annotation = null;
        }

        public AssignExp(Exp lhs, Exp annotation, Op op, Exp rhs, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Dst = lhs;
            this.Operator = op;
            this.Src = rhs;
            this.Annotation = annotation;
        }

        public Exp Dst { get; }
        public Op Operator { get; }
        public Exp Src { get; }
        public Exp? Annotation { get; }
    
        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitAssignExp(this, context);
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
                this.Annotation.Write(writer);
            }
            writer.Write(base.OpToString(Operator));
            if (Src != null)
            {
                Src.Write(writer);
            }
        }
    }

    public class PyTuple : Exp
    {
        public PyTuple(List<Exp> values, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Values = values;
        }

        public List<Exp> Values { get; }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitTuple(this, context);
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
            var sep = "";
            foreach (var e in Values)
            {
                writer.Write(sep);
                sep = ",";
                e.Write(writer);
            }
            writer.Write(")");
        }
    }

 /// <summary>
 /// virtual-AST node used to represent virtual source locations for builtins
 /// as external urls.
 /// </sumary>
    public class Url : Exp
    {

        public Url(string url) : base("", -1, -1)
        {
            this.Value = url;
        }

        public string Value { get; }

        public override void Write(TextWriter writer)
        {
            writer.Write("<Url:\"{0}\">", Value);
        }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            throw new NotImplementedException();
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

    // The := "walrus operator"
    public class AssignmentExp : Exp
    {
        public Identifier Dst;

        public Exp Src;

        public AssignmentExp(Identifier dst, Exp src, string filename, int start, int end) 
            : base(filename, start, end)
        {
            this.Dst = dst;
            this.Src = src;
        }

        public override T Accept<T, C>(IExpVisitor<T, C> v, C context)
        {
            return v.VisitAssignmentExp(this, context);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitAssignmentExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitAssignmentExp(this);
        }

        public override void Write(TextWriter writer)
        {
            Dst.Write(writer);
            writer.Write(" ");
            writer.Write(":=");
            writer.Write(" ");
            Src.Write(writer);
        }
    }
}
