namespace Pytocs.Core.Syntax
{
    public class YieldStatement : Statement
    {
        public Exp Expression;

        public YieldStatement(Exp exp, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            Expression = exp;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitYield(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitYield(this);
        }
    }
}