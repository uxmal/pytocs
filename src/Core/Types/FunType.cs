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

using Pytocs.Core.TypeInference;
using Pytocs.Core.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.Types
{
    public class FunType : DataType
    {
        public IDictionary<DataType, DataType> arrows = new Dictionary<DataType, DataType>();
        public readonly FunctionDef Definition;
        public Lambda lambda;
        public ClassType Class = null;
        public State env;
        public List<DataType> defaultTypes;       // types for default parameters (evaluated at def time)

        public FunType()
        {
            this.Class = null;
        }

        public FunType(FunctionDef func, State env)
        {
            this.Definition = func;
            this.env = env;
        }

        public FunType(Lambda lambda, State env)
        {
            this.lambda = lambda;
            this.env = env;
        }

        public FunType(DataType from, DataType to)
        {
            AddMapping(from, to);
        }

        public DataType SelfType { get; set; }                 // self's type for calls

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitFun(this);
        }

        public void AddMapping(DataType from, DataType to)
        {
            if (from is TupleType tuple)
            {
                from = SimplifySelf(tuple);
            }

            if (arrows.Count < 5)
            {
                arrows[from] = to;
                var oldArrows = this.arrows;
                this.arrows = CompressArrows(arrows);

                if (arrows.Count > 10)
                {
                    this.arrows = oldArrows;
                }
            }
        }

        public DataType GetMapping(DataType from)
        {
            return arrows.TryGetValue(from, out var to)
                ? to
                : null;
        }

        public DataType GetReturnType()
        {
            if (arrows.Count != 0)
            {
                return arrows.Values.First();
            }
            else
            {
                return DataType.Unknown;
            }
        }

        public void SetDefaultTypes(List<DataType> defaultTypes)
        {
            this.defaultTypes = defaultTypes;
        }

        public override bool Equals(object other)
        {
            if (other is FunType fo)
            {
                return fo.Table.Path.Equals(Table.Path) || object.ReferenceEquals(this , other);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return "FunType".GetHashCode();
        }

        /// <summary>
        /// Create a new FunType which is an awaitable version of this FunType.
        /// </summary>
        public FunType MakeAwaitable()
        {
            var fnAwaitable = new FunType(this.Definition, this.env)
            {
                arrows = this.arrows.ToDictionary(k => k.Key, v => (DataType)new AwaitableType(v.Value)),
                lambda = this.lambda,
                Class = this.Class,
                defaultTypes = this.defaultTypes
            };
            return fnAwaitable;
        }

        private bool Subsumed(DataType type1, DataType type2)
        {
            return SubsumedInner(type1, type2, new TypeStack());
        }

        private bool SubsumedInner(DataType type1, DataType type2, TypeStack typeStack)
        {
            if (typeStack.Contains(type1, type2))
            {
                return true;
            }

            if (type1.isUnknownType() || type1 == DataType.None || type1.Equals(type2))
            {
                return true;
            }

            if (type1 is TupleType && type2 is TupleType)
            {
                List<DataType> elems1 = ((TupleType) type1).eltTypes;
                List<DataType> elems2 = ((TupleType) type2).eltTypes;

                if (elems1.Count == elems2.Count)
                {
                    typeStack.Push(type1, type2);
                    for (int i = 0; i < elems1.Count; i++)
                    {
                        if (!SubsumedInner(elems1[i], elems2[i], typeStack))
                        {
                            typeStack.Pop(type1, type2);
                            return false;
                        }
                    }
                }
                return true;
            }

            if (type1 is ListType list1 && type2 is ListType list2)
            {
                return SubsumedInner(list1.ToTupleType(), list2.ToTupleType(), typeStack);
            }
            return false;
        }

        private IDictionary<DataType, DataType> CompressArrows(IDictionary<DataType, DataType> arrows)
        {
            IDictionary<DataType, DataType> ret = new Dictionary<DataType, DataType>();
            foreach (var e1 in arrows)
            {
                bool fSubsumed = false;
                foreach (var e2 in arrows)
                {
                    if (e1.Key != e2.Key && Subsumed(e1.Key, e2.Key))
                    {
                        fSubsumed = true;
                        break;
                    }
                }
                if (!fSubsumed)
                {
                    ret[e1.Key] = e1.Value;
                }
            }
            return ret;
        }

        /// <summary>
        /// If the self type is set, use the self type in the display
        /// This is for display purpose only, it may not be logically
        /// correct wrt some pathological programs
        /// </summary>
        private TupleType SimplifySelf(TupleType from)
        {
            TupleType simplified = new TupleType();     //$NO regs
            if (from.eltTypes.Count > 0)
            {
                if (Class != null)
                {
                    simplified.add(Class.GetInstance());
                }
                else
                {
                    simplified.add(from.get(0));
                }
            }

            for (int i = 1; i < from.eltTypes.Count; i++)
            {
                simplified.add(from.get(i));
            }
            return simplified;
        }
    }
}