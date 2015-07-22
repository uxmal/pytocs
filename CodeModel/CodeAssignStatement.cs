using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeAssignStatement : CodeStatement
    {
        public CodeAssignStatement(CodeExpression lhs, CodeExpression rhs)
        {
            this.Destination = lhs;
            this.Source = rhs;
        }

        public CodeExpression Destination { get; set; }
        public CodeExpression Source { get; set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitAssignment(this);
        }
    }
}
