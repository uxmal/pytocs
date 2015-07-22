using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeUnaryOperatorExpression : CodeExpression
    {
        public CodeUnaryOperatorExpression(CodeOperatorType codeOperatorType, CodeExpression e)
        {
            this.Operator = codeOperatorType;
            this.Expression = e;
        }

        public CodeOperatorType Operator { get; set; }
        public CodeExpression Expression { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitUnary(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitUnary(this);
        }
    }
}
