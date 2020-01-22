namespace Pytocs.Core.Syntax
{
    public class WhileStatement : Statement
    {
        public SuiteStatement Body;
        public SuiteStatement Else;
        public Exp Test;

        public WhileStatement(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitWhile(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitWhile(this);
        }
    }
}