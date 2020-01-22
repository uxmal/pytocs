namespace Pytocs.Core.Syntax
{
    public class ExpStatement : Statement
    {
        public readonly Exp Expression;

        public ExpStatement(Exp e, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            Expression = e;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitExp(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitExp(this);
        }
    }
}