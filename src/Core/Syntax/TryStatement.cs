using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class TryStatement : Statement
    {
        public SuiteStatement body;
        public Statement elseHandler;
        public List<ExceptHandler> exHandlers;
        public Statement finallyHandler;

        public TryStatement(
            SuiteStatement body,
            List<ExceptHandler> exHandlers,
            Statement elseHandler,
            Statement finallyHandler,
            string filename,
            int start,
            int end)
            : base(filename, start, end)
        {
            this.body = body;
            this.exHandlers = exHandlers;
            this.elseHandler = elseHandler;
            this.finallyHandler = finallyHandler;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitTry(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitTry(this);
        }
    }
}