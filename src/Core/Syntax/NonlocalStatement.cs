using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class NonlocalStatement : Statement
    {
        public List<Identifier> names;

        public NonlocalStatement(List<Identifier> names, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.names = names;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitNonLocal(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitNonLocal(this);
        }
    }
}