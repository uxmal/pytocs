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

using State = Pytocs.Core.TypeInference.State;

namespace Pytocs.Core.Types
{
    public class ModuleType : DataType
    {
        public string name;
        public string qname;

        public ModuleType(string name, string file, string qName, State parent)
        {
            this.name = name;
            this.file = file;  // null for builtin modules
            this.qname = qName;
            this.Table = new State(parent, State.StateType.MODULE);
            Table.Path = qname;
            Table.DataType = this;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitModule(this);
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is ModuleType co)
            {
                if (file != null)
                {
                    return file.Equals(co.file);
                }
            }
            return object.ReferenceEquals(this, other);
        }
    }
}
