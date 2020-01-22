using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class WithStatement : Statement
    {
        public SuiteStatement body;
        public List<WithItem> items;

        public WithStatement(List<WithItem> ws, SuiteStatement s, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            items = ws;
            body = s;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitWith(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitWith(this);
        }
    }
}