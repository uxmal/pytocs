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

namespace Pytocs.Core.Types
{
    public class UnionType : DataType
    {
        public ISet<DataType> types;

        public UnionType()
        {
            this.types = new HashSet<DataType>();
        }

        public UnionType(params DataType[] initialTypes) :
            this()
        {
            foreach (DataType nt in initialTypes)
            {
                AddType(nt);
            }
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitUnion(this);
        }

        /**
         * Returns true if t1 == t2 or t1 is a union type that contains t2.
         */
        static public bool Contains(DataType t1, DataType t2)
        {
            if (t1 is UnionType)
            {
                return ((UnionType) t1).Contains(t2);
            }
            else
            {
                return t1.Equals(t2);
            }
        }


        public static DataType Remove(DataType t1, DataType t2)
        {
            if (t1 is UnionType u)
            {
                ISet<DataType> types = new HashSet<DataType>(u.types);
                types.Remove(t2);
                return UnionType.CreateUnion(types);
            }
            else if (t1 != DataType.Unit && t1 == t2)
            {
                return DataType.Unknown;
            }
            else
            {
                return t1;
            }
        }

        static public DataType CreateUnion(IEnumerable<DataType> types)
        {
            DataType t = DataType.Unknown;
            foreach (DataType nt in types)
            {
                t = Union(t, nt);
            }
            return t;
        }

        public void SetTypes(ISet<DataType> types)
        {
            this.types = types;
        }

        public void AddType(DataType t)
        {
            if (t is UnionType ut)
            {
                types.UnionWith(ut.types);
            }
            else
            {
                types.Add(t);
            }
        }

        public bool Contains(DataType t)
        {
            return types.Contains(t);
        }

        /// <summary>
        /// Make the a union of two types
        /// with preference: other > None > Cont > unknown
        /// </summary>
        public static DataType Union(DataType u, DataType v)
        {
            if (u.Equals(v))
            {
                return u;
            }
            else if (u != DataType.Unknown && v == DataType.Unknown)
            {
                return u;
            }
            else if (v != DataType.Unknown && u == DataType.Unknown)
            {
                return v;
            }
            else if (u != DataType.None && v == DataType.None)
            {
                return u;
            }
            else if (v != DataType.None && v == DataType.None)
            {
                return v;
            }
            else if (u is IntType && v is FloatType)
            {
                return v;
            }
            else if (u is FloatType && v is IntType)
            {
                return u;
            }
            else if (u is TupleType tu && v is TupleType tv)
            {
                return UnifyTuples(tu, tv);
            }
            else
            {
                return new UnionType(u, v);
            }
        }

        /// <summary>
        /// Unifies two tuples. 
        /// </summary>
        /// <param name="tu"></param>
        /// <param name="tv"></param>
        /// <returns></returns>
        private static TupleType UnifyTuples(TupleType tu, TupleType tv)
        {
            var common = Math.Min(tu.eltTypes.Length, tv.eltTypes.Length);
            var elts = new List<DataType>();
            for (int i = 0; i < common; ++i)
            {
                elts.Add(Union(tu.eltTypes[i], tv.eltTypes[i]));
            }
            return new TupleType(true, elts.ToArray());
        }


        /// <summary>
        /// Returns the first alternate whose type is not unknown and
        /// is not None.
        /// 
        /// @return the first non-unknown, non-{@code None} alternate, or {@code null} if none found
        /// </summary>
        public DataType? FirstUseful()
        {
            return types
                .Where(type => (!type.IsUnknownType() && type != DataType.None))
                .FirstOrDefault();
        }

        public override bool Equals(object? other)
        {
            if (other is not DataType that)
                return false;
            if (typeStack.Contains(this, that))
            {
                return true;
            }
            else if (other is UnionType u)
            {
                ISet<DataType> types1 = types;
                ISet<DataType> types2 = u.types;
                if (types1.Count != types2.Count)
                {
                    return false;
                }
                else
                {
                    typeStack.Push(this, that);
                    foreach (DataType t in types2)
                    {
                        if (!types1.Contains(t))
                        {
                            typeStack.Pop(this, other);
                            return false;
                        }
                    }
                    foreach (DataType t in types1)
                    {
                        if (!types2.Contains(t))
                        {
                            typeStack.Pop(this, other);
                            return false;
                        }
                    }
                    typeStack.Pop(this, other);
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return "UnionType".GetHashCode();
        }

        public override DataType MakeGenericType(params DataType[] typeArguments)
        {
            return new UnionType(typeArguments);
        }
    }
}