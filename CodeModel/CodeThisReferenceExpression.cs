using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeThisReferenceExpression : CodeExpression
    {
        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitThisReference(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitThisReference(this);
        }
    }
}
