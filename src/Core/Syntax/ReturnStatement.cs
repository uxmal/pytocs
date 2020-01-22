namespace Pytocs.Core.Syntax
{
    public class ReturnStatement : Statement
    {
        public readonly Exp Expression;

        public ReturnStatement(string filename, int pos, int end) : base(filename, pos, end)
        {
            Expression = null;
        }

        public ReturnStatement(Exp e, string filename, int pos, int end) : base(filename, pos, end)
        {
            Expression = e;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitReturn(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitReturn(this);
        }
    }
}