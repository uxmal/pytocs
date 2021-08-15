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
    public class CodeBinaryOperatorExpression : CodeExpression
    {
        public CodeExpression Left { get; set; }
        public CodeOperatorType Operator { get; set; }
        public CodeExpression Right { get; set; }

        public CodeBinaryOperatorExpression(CodeExpression l, CodeOperatorType op, CodeExpression r)
        {
            this.Left = l;
            this.Operator = op;
            this.Right = r;
        }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitBinary(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitBinary(this);
        }
    }
}
