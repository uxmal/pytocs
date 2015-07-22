using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeLambdaExpression : CodeExpression
    {
        public CodeLambdaExpression(CodeExpression[] args, CodeExpression expr)
        {
            this.Arguments = args;
            this.Body = expr;
        }

        public CodeExpression[] Arguments { get; set; }
        public CodeExpression Body { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitLambda(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitLambda(this);
        }
    }
}
