namespace Pytocs.Core.Syntax
{
    public class PassStatement : Statement
    {
        public PassStatement(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitPass(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitPass(this);
        }
    }
}