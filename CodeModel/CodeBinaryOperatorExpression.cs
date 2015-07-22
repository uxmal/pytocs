using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
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
