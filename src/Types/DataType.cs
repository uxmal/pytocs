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

using System;
using System.Collections.Generic;
using State = Pytocs.TypeInference.State;
using TypeStack = Pytocs.TypeInference.TypeStack;

namespace Pytocs.Types
{
    public abstract class DataType
    {
        public string file = null;

        protected static TypeStack typeStack = new TypeStack();

        public DataType()
        {
            this.Table = new State(null, State.StateType.SCOPE);
        }

        public State Table { get; set; }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }

        public static bool operator ==(DataType a, DataType b)
        {
            if (object.ReferenceEquals(a ,null))
                return object.ReferenceEquals(b , null);
            return a.Equals(b);
        }

        public static bool operator !=(DataType a, DataType b)
        {
            if (object.ReferenceEquals(a, null))
                return !object.ReferenceEquals(b, null);
            return !a.Equals(b);
        }
    
        public void setFile(string file)
        {
            this.file = file;
        }

        public bool isNumType()
        {
            return this is IntType || this is FloatType;
        }

        public bool isUnknownType()
        {
            return this == DataType.Unknown;
        }

        public ModuleType asModuleType()
        {
            if (this is UnionType)
            {
                foreach (DataType t in ((UnionType) this).types)
                {
                    if (t is ModuleType)
                    {
                        return t.asModuleType();
                    }
                }
                throw new InvalidOperationException("Does not contain a ModuleType.");
                // can't get here, just to make the annotation happy
                //return new ModuleType(null, null, null, null);
            }
            else if (this is ModuleType)
            {
                return (ModuleType) this;
            }
            else
            {
                throw new InvalidOperationException("This is not a ModuleType.");
                // can't get here, just to make the annotation happy
                //return new ModuleType(null, null, null, null);
            }
        }

        public abstract T Accept<T>(IDataTypeVisitor<T> visitor);

        public override string ToString()
        {
            return this.Accept(new TypePrinter());
        }

        public static readonly InstanceType Unknown = new InstanceType(new ClassType("?", null, null));
        public static readonly InstanceType Cont = new InstanceType(new ClassType("None", null, null));
        public static readonly InstanceType None = new InstanceType(new ClassType("None", null, null));
        public static readonly StrType Str = new StrType(null);
        public static readonly IntType Int = new IntType();
        public static readonly FloatType Float = new FloatType();
        public static readonly ComplexType Complex = new ComplexType();
        public static readonly BoolType Bool = new BoolType(BoolType.Value.Undecided);
    }
}