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
    public class CodeConditionExpression : CodeExpression
    {
        public CodeExpression Condition;
        public CodeExpression Consequent;
        public CodeExpression Alternative;

        public CodeConditionExpression(CodeExpression cond, CodeExpression cons, CodeExpression alt)
        {
            this.Condition = cond;
            this.Consequent = cons;
            this.Alternative = alt;
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitCondition(this);
        }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitCondition(this);
        }
    }
}
