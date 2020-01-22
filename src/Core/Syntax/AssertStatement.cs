using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class AssertStatement : Statement
    {
        public AssertStatement(List<Exp> e, string filename, int start, int end)
            : base(filename, start, end)
        {
            Tests = e;
        }

        public List<Exp> Tests { get; set; }
        public Exp Message { get; set; } //$TODO: initialize this?

        public override void Accept(IStatementVisitor v)
        {
            v.VisitAssert(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitAssert(this);
        }
    }
}