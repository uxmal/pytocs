namespace Pytocs.Core.Syntax
{
    public class ContinueStatement : Statement
    {
        public ContinueStatement(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitContinue(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitContinue(this);
        }
    }
}