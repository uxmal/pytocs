namespace Pytocs.Core.Syntax
{
    public class AsyncStatement : Statement
    {
        public Statement Statement;

        public AsyncStatement(Statement stmt, string filename, int start, int end)
            : base(filename, start, end)
        {
            Statement = stmt;
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitAsync(this);
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitAsync(this);
        }
    }
}