using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class SuiteStatement : Statement
    {
        public readonly List<Statement> stmts;

        public SuiteStatement(List<Statement> stmts, string filename, int pos, int end) : base(filename, pos, end)
        {
            this.stmts = stmts;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitSuite(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitSuite(this);
        }
    }
}