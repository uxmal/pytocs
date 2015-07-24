#region License
//  Copyright 2015 John Källén
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

using Analyzer = Pytocs.TypeInference.Analyzer;

namespace Pytocs.Types
{
    public class DictType : DataType
    {
        public DataType keyType;
        public DataType valueType;

        public DictType(DataType key0, DataType val0)
        {
            keyType = key0;
            valueType = val0;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitDict(this);
        }
        
        public void add(DataType key, DataType val)
        {
            keyType = UnionType.union(keyType, key);
            valueType = UnionType.union(valueType, val);
        }

        public TupleType toTupleType(int n)
        {
            TupleType ret = new TupleType();        //$ NO registation. Badness?
            for (int i = 0; i < n; i++)
            {
                ret.add(keyType);
            }
            return ret;
        }

        public override bool Equals(object other)
        {
            var dtOther = other as DataType;
            if (dtOther == null)
                return false;
            if (typeStack.contains(this, dtOther))
            {
                return true;
            }
            else if (other is DictType)
            {
                typeStack.push(this, dtOther);
                DictType co = (DictType) other;
                bool ret = (co.keyType.Equals(keyType) &&
                        co.valueType.Equals(valueType));
                typeStack.pop(this, other);
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


    }
}