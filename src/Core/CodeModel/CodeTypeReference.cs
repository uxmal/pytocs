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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.CodeModel
{
    public class CodeTypeReference
    {
        public CodeTypeReference(Type type)
        {
            TypeArguments = new List<CodeTypeReference>();
            if (type.IsGenericType)
            {
                TypeName = type.FullName!.Remove(type.FullName.IndexOf('`'));
                TypeArguments = type.GetGenericArguments()
                    .Select(tArg => new CodeTypeReference(tArg))
                    .ToList();
            }
            else
            {
                TypeName = type.ToString();
            }
        }

        public CodeTypeReference(CodeTypeReference arrayElementType, int rank) 
            : this(arrayElementType.TypeName)
        {
            this.ArrayRank = rank;
            this.TypeArguments = arrayElementType.TypeArguments;
        }

        public CodeTypeReference(string typeName)
        {
            TypeArguments = new List<CodeTypeReference>();
            TypeName = typeName;
        }

        public CodeTypeReference(string typeName, params CodeTypeReference[] typeArgs)
        {
            TypeArguments = new List<CodeTypeReference>();
            this.TypeName = typeName;
            this.TypeArguments.AddRange(typeArgs);
        }

        public int ArrayRank { get; set; }
        public string TypeName { get; set; }
        public List<CodeTypeReference> TypeArguments { get; private set; }
    }
}
