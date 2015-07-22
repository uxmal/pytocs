using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeTryCatchFinallyStatement : CodeStatement
    {
        public List<CodeStatement> TryStatements { get; private set; }
        public List<CodeCatchClause>    CatchClauses  { get; private set; }
        public List<CodeStatement>    FinallyStatements  { get; private set; }

        public CodeTryCatchFinallyStatement()
        {
            TryStatements = new List<CodeStatement>();
            CatchClauses = new List<CodeCatchClause>();
            FinallyStatements = new List<CodeStatement>();
        }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitTry(this);
        }
    }
}
