using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeParameterDeclarationExpression : CodeExpression
    {
        public CodeParameterDeclarationExpression()
        {
        }

        public CodeParameterDeclarationExpression(Type type, string name)
        {
            this.ParameterType = new CodeTypeReference(type);
            this.ParameterName = name;
        }

        public CodeParameterDeclarationExpression(Type type, string name, CodeExpression defaultValue) : this(type, name)
        {
            this.DefaultValue = defaultValue;
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
