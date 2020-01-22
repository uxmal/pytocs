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

using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.Types
{
    public class UnionType : DataType
    {
        public ISet<DataType> types;

        public UnionType()
        {
            types = new HashSet<DataType>();
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

        public static bool Contains(DataType t1, DataType t2)
        {
            if (t1 is UnionType)
            {
                return ((UnionType)t1).Contains(t2);
            }

            return t1.Equals(t2);
        }

        public static DataType Remove(DataType t1, DataType t2)
        {
            if (t1 is UnionType u)
            {
                ISet<DataType> types = new HashSet<DataType>(u.types);
                types.Remove(t2);
                return CreateUnion(types);
            }

            if (t1 != Cont && t1 == t2)
            {
                return Unknown;
            }

            return t1;
        }

        public static DataType CreateUnion(IEnumerable<DataType> types)
        {
            DataType t = Unknown;
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
        ///     Make the a union of two types
        ///     with preference: other > None > Cont > unknown
        /// </summary>
        public static DataType Union(DataType u, DataType v)
        {
            if (u.Equals(v))
            {
                return u;
            }

            if (u != Unknown && v == Unknown)
            {
                return u;
            }

            if (v != Unknown && u == Unknown)
            {
                return v;
            }

            if (u != None && v == None)
            {
                return u;
            }

            if (v != None && v == None)
            {
                return v;
            }

            if (u is IntType && v is FloatType)
            {
                return v;
            }

            if (u is FloatType && v is IntType)
            {
                return u;
            }

            return new UnionType(u, v);
        }

        /// <summary>
        ///     Returns the first alternate whose type is not unknown and
        ///     is not None.
        ///     @return the first non-unknown, non-{@code None} alternate, or {@code null} if none found
        /// </summary>
        public DataType FirstUseful()
        {
            return types
                .Where(type => !type.IsUnknownType() && type != None)
                .FirstOrDefault();
        }

        public override bool Equals(object other)
        {
            DataType dtOther = other as DataType;
            if (dtOther == null)
            {
                return false;
            }

            if (typeStack.Contains(this, dtOther))
            {
                return true;
            }

            if (other is UnionType)
            {
                ISet<DataType> types1 = types;
                ISet<DataType> types2 = ((UnionType)other).types;
                if (types1.Count != types2.Count)
                {
                    return false;
                }

                typeStack.Push(this, dtOther);
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

            return false;
        }

        public override int GetHashCode()
        {
            return "UnionType".GetHashCode();
        }
    }
}