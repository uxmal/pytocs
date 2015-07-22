using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeForeachStatement : CodeStatement
    {
        public CodeForeachStatement(CodeExpression exp, CodeExpression list)
        {
            this.Variable = exp;
            this.Collection = list;
            this.Statements = new List<CodeStatement>();
        }

        public CodeExpression Variable { get; set; }
        public CodeExpression Collection { get; set; }
        public List<CodeStatement> Statements { get; private set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitForeach(this);
        }
    }
}
