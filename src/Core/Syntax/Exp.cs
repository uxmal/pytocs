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
}