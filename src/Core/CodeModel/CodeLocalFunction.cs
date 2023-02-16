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
    public class CodeLocalFunction : CodeStatement, ICodeFunction
    {
        public CodeLocalFunction()
        {
            this.Parameters = new List<CodeParameterDeclarationExpression>();
            this.Statements = new List<CodeStatement>();
            this.Comments = new List<CodeCommentStatement>();
        }

        public bool IsAsync { get; set; }
        public CodeTypeReference? ReturnType { get; set; }
        public string? Name { get; set; }

        public List<CodeParameterDeclarationExpression> Parameters { get; }
        public List<CodeStatement> Statements { get; }
        public List<CodeCommentStatement> Comments { get; set; }


        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitLocalFunction(this);
        }
    }
}
