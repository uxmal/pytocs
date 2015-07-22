using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodePrimitiveExpression : CodeExpression
    {
        public CodePrimitiveExpression(object o)
        {
            this.Value = o;
        }

        public object Value { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitPrimitive(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitPrimitive(this);
        }
    }
}
