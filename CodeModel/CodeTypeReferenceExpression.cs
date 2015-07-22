using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeTypeReferenceExpression : CodeExpression
    {
        public CodeTypeReferenceExpression(string typeName)
        {
            this.TypeName = typeName;
        }

        public string TypeName { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitTypeReference(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitTypeReference(this);
        }
    }
}
