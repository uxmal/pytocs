namespace Pytocs.Core.Syntax
{
    public class BreakStatement : Statement
    {
        public BreakStatement(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitBreak(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitBreak(this);
        }
    }
}