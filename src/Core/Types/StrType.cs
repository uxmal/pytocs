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

namespace Pytocs.Core.Types
{
    public class StrType : DataType
    {
        public string value;

        public StrType(string value)
        {
            this.value = value;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitStr(this);
        }

        public override bool Equals(object other)
        {
            return (other is StrType);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }
    }
}