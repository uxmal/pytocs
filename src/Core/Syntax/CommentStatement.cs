namespace Pytocs.Core.Syntax
{
    public class CommentStatement : Statement
    {
        public CommentStatement(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitComment(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitComment(this);
        }
    }
}