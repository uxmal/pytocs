#region License
//  Copyright 2015-2022 John Källén
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
using NameScope = Pytocs.Core.TypeInference.NameScope;
using NameScopeType = Pytocs.Core.TypeInference.NameScopeType;

namespace Pytocs.Core.Types
{
    public class ClassType : DataType
    {
        private DataType[]? genericArguments;


        public ClassType(string name, NameScope? parent, string? path)
        {
            this.name = name;
            this.Scope = new NameScope(parent, NameScopeType.CLASS) { DataType = this };
            if (parent != null)
            {
                Scope.Path = path ?? "";
            }
            else
            {
                Scope.Path = name;
            }
        }

        public ClassType(string name, NameScope parent, string path, ClassType? superClass)
            : this(name, parent, path)
        {
            if (!(superClass is null))
            {
                AddSuper(superClass);
            }
        }

        public static ClassType CreateUnboundGeneric(string name, int arity, NameScope parent, string? path)
        {
            var ct = new ClassType(name, parent, path);
            ct.genericArguments = new DataType[arity];
            return ct;
        }

        public string name;
        public InstanceType? instance;
        public bool IsClosedGenericType { get; set; }


        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitClass(this);
        }

        public void AddSuper(DataType superclass)
        {
            Scope.AddSuperClass(superclass.Scope);
        }

        public InstanceType GetInstance()
        {
            if (instance is null)
            {
                instance = new InstanceType(this);
            }
            return instance;
        }

        public InstanceType GetInstance(IList<DataType> args, DataType inferencer, Exp call)
        {
            if (instance is null)
            {
                var initArgs = args ?? new List<DataType>();
                this.instance = new InstanceType(this);
            }
            return instance;
        }

        public override bool Equals(object? other)
        {
            return object.ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override DataType MakeGenericType(params DataType[] typeArguments)
        {
            if (genericArguments is null)
                throw new InvalidOperationException("This class is not generic.");
            if (IsClosedGenericType)
                throw new InvalidOperationException("This generic class has been closed.");
            if (genericArguments.Length != typeArguments.Length)
                throw new ArgumentException(
                    $"Expected {genericArguments.Length} type arguments, but got {typeArguments.Length}.",
                    nameof(typeArguments));
            var ctClosed = new ClassType(this.name, Scope.Parent, this.Scope.Path);
            //$REVIEW: assume we own typeArguments. Otherwise we may need to copy.
            ctClosed.genericArguments = genericArguments;
            ctClosed.IsClosedGenericType = true;
            return ctClosed;
        }
    }
}
