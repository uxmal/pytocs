using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class GlobalStatement : Statement
    {
        public List<Identifier> names;

        public GlobalStatement(List<Identifier> names, string filename, int start, int end) : base(filename, start, end)
        {
            this.names = names;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitGlobal(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitGlobal(this);
        }
    }
}