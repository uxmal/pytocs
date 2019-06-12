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
using System.Text;
using Analyzer = Pytocs.Core.TypeInference.Analyzer;

namespace Pytocs.Core.Types
{
    public class ListType : DataType
    {
        public DataType eltType;
        public List<DataType> positional = new List<DataType>();
        public List<object> values = new List<object>();

        public ListType() : this(DataType.Unknown)
        {
        }

        public ListType(DataType elt)
        {
            eltType = elt;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitList(this);
        }

        public void setElementType(DataType eltType)
        {
            this.eltType = eltType;
        }

        public void Add(DataType another)
        {
            eltType = UnionType.Union(eltType, another);
            positional.Add(another);
        }

        public void addValue(object v)
        {
            values.Add(v);
        }

        public DataType get(int i)
        {
            return positional[i];
        }

        public TupleType ToTupleType(int n)
        {
            var ret = new List<DataType>();    //$ no regs
            for (int i = 0; i < n; i++)
            {
                ret.Add(eltType);
            }
            return new TupleType(ret.ToArray());
        }

        public TupleType ToTupleType()
        {
            return new TupleType(positional.ToArray());
        }

        public override bool Equals(object? other)
        {
            if (!(other is DataType dtOther))
                return false;
            if (typeStack.Contains(this, dtOther))
            {
                return true;
            }
            else if (other is ListType that)
            {
                typeStack.Push(this, that);
                bool ret = that.eltType.Equals(eltType);
                typeStack.Pop(this, other);
                return ret;
            }
            else
            {
                return false;
            }
        }


        public override int GetHashCode()
        {
            return "ListType".GetHashCode();
        }

        public override DataType MakeGenericType(params DataType[] typeArguments)
        {
            if (typeArguments.Length != 1)
                throw new ArgumentException("Should only be one argument.");
            return new ListType(typeArguments[0]);
        }

    }
}