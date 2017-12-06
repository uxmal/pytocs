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

using State = Pytocs.TypeInference.State;

namespace Pytocs.Types
{
    public class ClassType : DataType
    {
        public string name;
        public InstanceType canon;
        public DataType superclass;

        public ClassType(string name, State parent, string path)
        {
            this.name = name;
            this.Table = new State(parent, State.StateType.CLASS) { Type = this };
            if (parent != null)
            {
                Table.Path = path;
            }
            else
            {
                Table.Path = name;
            }
        }

        public ClassType(string name, State parent, string path, ClassType superClass)
            : this(name, parent, path)
        {
            if (superClass != null)
            {
                AddSuper(superClass);
            }
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitClass(this);
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public void AddSuper(DataType superclass)
        {
            this.superclass = superclass;
            Table.addSuper(superclass.Table);
        }

        public InstanceType getCanon()
        {
            if (canon == null)
            {
                canon = new InstanceType(this);
            }
            return canon;
        }

        public override bool Equals(object other)
        {
            return object.ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
