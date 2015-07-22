using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeUsingStatement : CodeStatement
    {
        public CodeUsingStatement()
        {
            Initializers = new List<CodeStatement>();
            Statements = new List<CodeStatement>();
        }

        [Obsolete]
        public CodeUsingStatement(CodeExpression name, CodeExpression value) : this()
        {
            Initializers.Add(new CodeAssignStatement(name, value));
        }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitUsing(this);
        }

        public List<CodeStatement> Initializers { get; private set; }
        public List<CodeStatement> Statements { get; private set; }

    }
}
