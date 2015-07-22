using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeThrowExceptionStatement : CodeStatement
    {
        public CodeThrowExceptionStatement()
        {
        }

        public CodeThrowExceptionStatement(CodeExpression e)
        {
            this.Expression = e;
        }

        public CodeExpression Expression { get; set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitThrow(this);
        }
    }
}
