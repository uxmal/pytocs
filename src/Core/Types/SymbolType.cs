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

namespace Pytocs.Core.Types
{
    public class SymbolType : DataType
    {
        public string name;

        public SymbolType(string name)
        {
            this.name = name;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitSymbol(this);
        }

        public override bool Equals(object other)
        {
            if (other is SymbolType that)
            {
                return this.name.Equals(that.name);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override DataType MakeGenericType(params DataType[] typeArguments)
        {
            throw new InvalidOperationException("StrType cannot be generic.");
        }
    }
}