﻿#region License

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
    public abstract class CodeMember
    {
        public CodeMember()
        {
            this.CustomAttributes = new List<CodeAttributeDeclaration>();
            this.Comments = new List<CodeCommentStatement>();
        }

        public string Name { get; set; }
        public List<CodeAttributeDeclaration> CustomAttributes { get; private set; }
        public MemberAttributes Attributes { get; set; }
        public List<CodeCommentStatement> Comments { get; private set; }

        public abstract T Accept<T>(ICodeMemberVisitor<T> visitor);
    }
}