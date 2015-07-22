using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeYieldStatement : CodeStatement
    {
        public CodeYieldStatement(CodeExpression exp)
        {
            this.Expression = exp;
        }

        public CodeExpression Expression { get; set; }


        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitYield(this);
        }
    }
}
