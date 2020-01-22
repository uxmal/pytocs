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

using System;

namespace Pytocs.Core.CodeModel
{
    public class CodeParameterDeclarationExpression : CodeExpression
    {
        public CodeParameterDeclarationExpression()
        {
        }

        public CodeParameterDeclarationExpression(CodeTypeReference type, string name)
        {
            ParameterType = type;
            ParameterName = name;
        }

        public CodeParameterDeclarationExpression(Type type, string name)
        {
            ParameterType = new CodeTypeReference(type);
            ParameterName = name;
        }

        public CodeParameterDeclarationExpression(Type type, string name, CodeExpression defaultValue) : this(type,
            name)
        {
            DefaultValue = defaultValue;
        }

        public CodeTypeReference ParameterType { get; set; }
        public string ParameterName { get; set; }
        public CodeExpression DefaultValue { get; set; }
        public bool IsVarargs { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitParameterDeclaration(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitParameterDeclaration(this);
        }
    }
}