using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class PrintStatement : Statement
    {
        public List<Argument> args;

        public Exp outputStream;
        public bool trailingComma;

        public PrintStatement(Exp outputStream, List<Argument> args, bool trailingComma, string filename, int pos,
            int end)
            : base(filename, pos, end)
        {
            this.outputStream = outputStream;
            this.args = args;
            this.trailingComma = trailingComma;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitPrint(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitPrint(this);
        }
    }
}