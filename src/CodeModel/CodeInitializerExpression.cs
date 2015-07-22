using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeInitializerExpression : CodeExpression
    {
        public CodeInitializerExpression(params CodeExpression[] values)
        {
            this.Values = values;
        }

        public CodeExpression[] Values { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitInitializer(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitInitializer(this);
        }
    }
}
