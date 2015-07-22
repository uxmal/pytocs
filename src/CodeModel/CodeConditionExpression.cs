using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeConditionExpression : CodeExpression
    {
        public CodeExpression Condition;
        public CodeExpression Consequent;
        public CodeExpression Alternative;

        public CodeConditionExpression(CodeExpression cond, CodeExpression cons, CodeExpression alt)
        {
            this.Condition = cond;
            this.Consequent = cons;
            this.Alternative = alt;
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitCondition(this);
        }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitCondition(this);
        }
    }
}
