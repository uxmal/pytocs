namespace Pytocs.Core.Syntax
{
    public class ForStatement : Statement
    {
        public readonly SuiteStatement Body;
        public readonly SuiteStatement Else;
        public readonly Exp exprs; // var or vars
        public readonly Exp tests; // iterator

        public ForStatement(
            Exp exprs,
            Exp tests,
            SuiteStatement body,
            SuiteStatement orelse,
            string filename,
            int start,
            int end)
            : base(filename, start, end)
        {
            this.exprs = exprs;
            this.tests = tests;
            Body = body;
            Else = orelse;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitFor(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitFor(this);
        }
    }
}