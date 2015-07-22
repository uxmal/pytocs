using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeFieldReferenceExpression : CodeExpression
    {

        public CodeFieldReferenceExpression(CodeExpression exp, string fieldName)
        {
            this.Expression = exp;
            this.FieldName = fieldName;
        }

        public CodeExpression Expression { get; set; }
        public string FieldName { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitFieldReference(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitFieldReference(this);
        }
    }
}
