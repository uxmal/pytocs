namespace Pytocs.Core.Syntax
{
    public class IfStatement : Statement
    {
        public readonly SuiteStatement Else;
        public readonly Exp Test;
        public readonly SuiteStatement Then;

        public IfStatement(
            Exp test,
            SuiteStatement then,
            SuiteStatement orelse,
            string filename, int start, int end)
            : base(filename, start, end)
        {
            Test = test;
            Then = then;
            Else = orelse;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitIf(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitIf(this);
        }
    }
}