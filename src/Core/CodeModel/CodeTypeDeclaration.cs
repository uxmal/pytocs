#region License

//  Copyright 2015-2020 John Källén
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

using System.Collections.Generic;

namespace Pytocs.Core.CodeModel
{
    public class CodeTypeDeclaration : CodeMember
    {
        public CodeTypeDeclaration(string name) : this()
        {
            Name = name;
        }

        public CodeTypeDeclaration()
        {
            Members = new List<CodeMember>();
            BaseTypes = new List<CodeTypeReference>();
            Attributes = MemberAttributes.Public;
        }

        public bool IsClass { get; set; }

        public List<CodeMember> Members { get; }

        public List<CodeTypeReference> BaseTypes { get; }

        public override T Accept<T>(ICodeMemberVisitor<T> visitor)
        {
            return visitor.VisitTypeDefinition(this);
        }
    }
}