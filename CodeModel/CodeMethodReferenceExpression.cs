using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeMethodReferenceExpression : CodeExpression
    {
        public CodeMethodReferenceExpression(CodeExpression r, string p)
        {
            this.TargetObject = r;
            this.MethodName = p;
        }

        public CodeExpression TargetObject { get; set; }
        public string MethodName { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitMethodReference(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitMethodReference(this);
        }
    }
}
