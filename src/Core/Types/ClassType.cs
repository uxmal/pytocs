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

using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using State = Pytocs.Core.TypeInference.State;

namespace Pytocs.Core.Types
{
    public class ClassType : DataType
    {
        public string name;
        public InstanceType instance;

        public ClassType(string name, State parent, string path)
        {
            this.name = name;
            this.Table = new State(parent, State.StateType.CLASS) { DataType = this };
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

        public void AddSuper(DataType superclass)
        {
            Table.addSuper(superclass.Table);
        }

        public InstanceType GetInstance()
        {
            if (instance == null)
            {
                instance = new InstanceType(this);
            }
            return instance;
        }

        public InstanceType GetInstance(IList<DataType> args, DataType inferencer, Exp call)
        {
            if (instance == null)
            {
                IList<DataType> initArgs = args ?? new List<DataType>();
                throw new NotImplementedException(" instance = new InstanceType(this, initArgs, inferencer, call);");
            }
            return instance;
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
