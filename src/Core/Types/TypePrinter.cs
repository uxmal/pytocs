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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Types
{
    public class TypePrinter : IDataTypeVisitor<string>
    {
        private readonly CyclicTypeRecorder ctr;
        private readonly bool multiline;
        private readonly bool showLiterals;

        public TypePrinter(bool multiline = false, bool showLiterals = false)
        {
            this.multiline = multiline;
            this.showLiterals = showLiterals;
            this.ctr = new CyclicTypeRecorder();
        }

        public string VisitAwaitable(AwaitableType awaitable)
        {
            return "awaitable(" + awaitable.ResultType.Accept(this) + ")";
        }

        public string VisitBool(BoolType b)
        {
            if (showLiterals)
                return "bool(" + b.value + ")";
            else
                return "bool";
        }

        public string VisitClass(ClassType c)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<").Append(c.name).Append(">");
            return sb.ToString();
        }

        public string VisitComplex(ComplexType c)
        {
            return "complex";
        }

        public string VisitDict(DictType d)
        {
            //        StringBuilder sb = new StringBuilder();
            //
            //        Integer num = ctr.visit(this);
            //        if (num != null) {
            //            sb.Append("#").append(num);
            //        } else {
            //            ctr.push(this);
            //            sb.Append("{");
            //            sb.Append(keyType.printType(ctr));
            //            sb.Append(" : ");
            //            sb.Append(valueType.printType(ctr));
            //            sb.Append("}");
            //            ctr.pop(this);
            //        }
            //
            //        return sb.toString();
            return "dict";
        }

        public string VisitFloat(FloatType f)
        {
            return "float";
        }

        public string VisitFun(FunType f)
        {
            if (f.arrows.Count == 0)
            {
                return "? -> ?";
            }

            StringBuilder sb = new StringBuilder();

            int? num = ctr.Visit(f);
            if (num.HasValue)
            {
                sb.Append("#").Append(num.Value);
            }
            else
            {
                int newNum = ctr.Push(f);

                int i = 0;
                ISet<string> seen = new HashSet<string>();

                foreach (var e in f.arrows)
                {
                    DataType from = e.Key;
                    string sArrow = $"{from.Accept(this)} -> {e.Value.Accept(this)}";
                    if (!seen.Contains(sArrow))
                    {
                        if (i != 0)
                        {
                            if (this.multiline)
                            {
                                sb.Append("\n| ");
                            }
                            else
                            {
                                sb.Append(" | ");
                            }
                        }

                        sb.Append(sArrow);
                        seen.Add(sArrow);
                    }
                    i++;
                }

                if (ctr.IsUsed(f))
                {
                    sb.Append("=#").Append(newNum).Append(": ");
                }
                ctr.Pop(f);
            }
            return sb.ToString();
        }

        public string VisitInstance(InstanceType i)
        {
            return ((ClassType) i.classType).name;
        }

        public string VisitInt(IntType i)
        {
            return "int";
        }

        public string VisitIterable(IterableType it)
        {
            return $"iter({it.ElementType.Accept(this)})";
        }
        public string VisitList(ListType l)
        {
            StringBuilder sb = new StringBuilder();

            int? num = ctr.Visit(l);
            if (num != null)
            {
                sb.Append("#").Append(num.Value);
            }
            else
            {
                ctr.Push(l);
                sb.Append("[");
                sb.Append(l.eltType.Accept(this));
                sb.Append("]");
                ctr.Pop(l);
            }
            return sb.ToString();
        }

        public string VisitModule(ModuleType m)
        {
            return m.Name;
        }

        public string VisitSet(SetType s)
        {
            return "{" + s.ElementType.Accept(this) + "}";
        }

        public string VisitStr(StrType s)
        {
            if (this.showLiterals)
            {
                return "str(" + s.value + ")";
            }
            else
            {
                return "str";
            }
        }

        public string VisitSymbol(SymbolType s)
        {
            return ":" + s.name;
        }

        public string VisitTuple(TupleType t)
        {
            StringBuilder sb = new StringBuilder();

            int? num = ctr.Visit(t);
            if (num != null)
            {
                sb.Append("#").Append(num.Value);
            }
            else
            {
                int newNum = ctr.Push(t);
                bool first = true;
                if (t.eltTypes.Length != 1)
                {
                    sb.Append("(");
                }

                foreach (DataType tt in t.eltTypes)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(tt.Accept(this));
                    first = false;
                }

                if (ctr.IsUsed(t))
                {
                    sb.Append("=#").Append(newNum).Append(":");
                }

                if (t.eltTypes.Length != 1)
                {
                    if (t.IsVariant)
                    {
                        sb.Append(", ...");
                    }    
                    sb.Append(")");
                }
                ctr.Pop(t);
            }
            return sb.ToString();
        }

        public string VisitUnion(UnionType u)
        {
            var sb = new StringBuilder();

            int? num = ctr.Visit(u);
            if (num is not null)
            {
                sb.Append('#').Append(num.Value);
            }
            else
            {
                int newNum = ctr.Push(u);
                bool first = true;
                sb.Append('{');

                foreach (DataType t in u.types)
                {
                    if (!first)
                    {
                        sb.Append(" | ");
                    }
                    sb.Append(t.Accept(this));
                    first = false;
                }

                if (ctr.IsUsed(u))
                {
                    sb.Append("=#").Append(newNum).Append(":");
                }

                sb.Append('}');
                ctr.Pop(u);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Internal class to support printing in the presence of type-graph cycles.
        /// </summary>
        protected class CyclicTypeRecorder
        {
            private readonly IDictionary<DataType, int> elements = new Dictionary<DataType, int>();
            private readonly ISet<DataType> used = new HashSet<DataType>();
            private int count = 0;

            public int Push(DataType t)
            {
                ++count;
                elements[t] = count;
                return count;
            }

            public void Pop(DataType t)
            {
                elements.Remove(t);
                used.Remove(t);
            }

            public int? Visit(DataType t)
            {
                if (elements.TryGetValue(t, out int i))
                {
                    used.Add(t);
                    return i;
                }
                else
                {
                    return null;
                }
            }

            public bool IsUsed(DataType t)
            {
                return used.Contains(t);
            }
        }
    }
}