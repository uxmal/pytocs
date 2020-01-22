namespace Pytocs.Core.Syntax
{
    public class RaiseStatement : Statement
    {
        public Exp exOriginal;

        public Exp exToRaise;
        public Exp traceback;

        public RaiseStatement(Exp exToRaise, Exp exOriginal, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.exToRaise = exToRaise;
            this.exOriginal = exOriginal;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitRaise(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitRaise(this);
        }
    }
}