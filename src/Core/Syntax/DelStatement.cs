namespace Pytocs.Core.Syntax
{
    public class DelStatement : Statement
    {
        public Exp Expressions;

        public DelStatement(Exp e, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            Expressions = e;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitDel(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitDel(this);
        }
    }
}