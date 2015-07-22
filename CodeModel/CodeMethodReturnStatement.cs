using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeMethodReturnStatement : CodeStatement
    {
        public CodeMethodReturnStatement()
        {
        }

        public CodeMethodReturnStatement(CodeExpression e)
        {
            this.Expression = e;
        }

        public CodeExpression Expression { get; set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitReturn(this);
        }

    }
}
