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

namespace Pytocs.Core.Types
{
    public class DictType : DataType
    {
        public DataType KeyType;
        public DataType ValueType;

        public DictType(DataType key, DataType val)
        {
            KeyType = key;
            ValueType = val;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitDict(this);
        }
        
        public void Add(DataType key, DataType val)
        {
            KeyType = UnionType.Union(KeyType, key);
            ValueType = UnionType.Union(ValueType, val);
        }

        public TupleType ToTupleType(int n)
        {
            
            var ret = new List<DataType>(); //$ NO registation. Badness?
            for (int i = 0; i < n; i++)
            {
                ret.Add(KeyType);
            }
            return new TupleType(ret.ToArray());
        }

        public override bool Equals(object? other)
        {
            if (!(other is DataType dtOther))
                return false;
            if (typeStack.Contains(this, dtOther))
            {
                return true;
            }
            else if (other is DictType co)
            {
                typeStack.Push(this, dtOther);
                bool ret = (co.KeyType.Equals(KeyType) &&
                            co.ValueType.Equals(ValueType));
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
            return "DictType".GetHashCode();
        }

        public override DataType MakeGenericType(params DataType[] typeArguments)
        {
            if (typeArguments.Length != 2)
                throw new ArgumentException("DictType expects two type arguments.");
            return new DictType(typeArguments[0], typeArguments[1]);
        }
    }
}