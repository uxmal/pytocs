#region License

//  Copyright 2015-2020 John K�ll�n
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

using State = Pytocs.Core.TypeInference.State;
using TypeStack = Pytocs.Core.TypeInference.TypeStack;

namespace Pytocs.Core.Types
{
    public abstract class DataType
    {
        public string file = null;

        protected static TypeStack typeStack = new TypeStack();

        protected DataType(State.StateType scopeType = State.StateType.SCOPE)
        {
            this.Table = new State(null, scopeType);
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
            if (a is null)
                return b is null;
            return a.Equals(b);
        }

        public static bool operator !=(DataType a, DataType b)
        {
            if (a is null)
                return !(b is null);
            return !a.Equals(b);
        }

        public bool IsNumType()
        {
            return this is IntType || this is FloatType;
        }

        public bool IsUnknownType()
        {
            return this == DataType.Unknown;
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