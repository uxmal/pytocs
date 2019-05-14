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

//using Analyzer = Pytocs.Core.TypeInference.Analyzer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pytocs.Core.Types
{
    /// <summary>
    /// Represents a Python tuple.
    /// </summary>
    public class TupleType : DataType
    {
        public List<DataType> eltTypes;

        public TupleType()
        {
            this.eltTypes = new List<DataType>();
        }

        public TupleType(List<DataType> eltTypes)
        {
            this.eltTypes = eltTypes;
        }

        public TupleType(DataType elt0)
            : this()
        {
            this.eltTypes.Add(elt0);
        }

        public TupleType(params DataType[] types)
            : this()
        {
            this.eltTypes.AddRange(types);
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitTuple(this);
        }

        public void add(DataType elt)
        {
            eltTypes.Add(elt);
        }

        public DataType get(int i)
        {
            return eltTypes[i];
        }

        public ListType ToListType()
        {
            ListType t = new ListType();        //$ no call to factory.
            foreach (DataType e in eltTypes)
            {
                t.Add(e);
            }
            return t;
        }

        public override bool Equals(object other)
        {
            if (!(other is DataType dtOther))
                return false;
            if (typeStack.Contains(this, dtOther))
            {
                return true;
            }
            else if (other is TupleType that)
            {
                List<DataType> types1 = eltTypes;
                List<DataType> types2 = that.eltTypes;

                if (types1.Count != types2.Count)
                    return false;
                typeStack.Push(this, dtOther);
                for (int i = 0; i < types1.Count; i++)
                {
                    if (!types1[i].Equals(types2[i]))
                    {
                        typeStack.Pop(this, other);
                        return false;
                    }
                }
                typeStack.Pop(this, other);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return "TupleType".GetHashCode();
        }
    }
}