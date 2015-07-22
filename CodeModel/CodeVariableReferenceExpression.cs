using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeVariableReferenceExpression : CodeExpression
    {
        public string Name { get; set; }

        public CodeVariableReferenceExpression(string name)
        {
            this.Name = name;
        }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitVariableReference(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitVariableReference(this);
        }
    }
}
