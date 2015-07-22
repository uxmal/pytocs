using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeNamedArgument : CodeExpression
    {
        public CodeExpression exp1;
        public CodeExpression exp2;

        public CodeNamedArgument(CodeExpression exp1, CodeExpression exp2)
        {
            this.exp1 = exp1;
            this.exp2 = exp2;
        }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitNamedArgument(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitNamedArgument(this);
        }
    }
}
