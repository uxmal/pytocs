using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeArrayIndexerExpression : CodeExpression
    {
        public CodeArrayIndexerExpression(CodeExpression exp, CodeExpression[] indices)
        {
            this.TargetObject = exp;
            this.Indices = indices;
        }
        
        public CodeExpression TargetObject { get; set; }
        public CodeExpression[] Indices { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitArrayIndexer(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitArrayIndexer(this);
        }
    }
}
