namespace Pytocs.Core.Syntax
{
    public class ExecStatement : Statement
    {
        public Exp code;
        public Exp globals;
        public Exp locals;

        public ExecStatement(Exp code, Exp globals, Exp locals, string filename, int pos, int end) : base(filename, pos,
            end)
        {
            this.code = code;
            this.globals = globals;
            this.locals = locals;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitExec(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitExec(this);
        }
    }
}