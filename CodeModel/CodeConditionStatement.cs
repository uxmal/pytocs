using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeConditionStatement : CodeStatement
    {
        public CodeConditionStatement()
        {
            TrueStatements = new List<CodeStatement>();
            FalseStatements = new List<CodeStatement>();
        }

        public CodeExpression Condition { get; set; }
        public List<CodeStatement> TrueStatements { get; private set; }
        public List<CodeStatement> FalseStatements { get; private set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitIf(this);
        }
    }
}
