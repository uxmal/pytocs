using System.IO;

namespace Pytocs.Core.Syntax
{
    public class UnaryExp : Exp
    {
        public Exp e;       //$TODO: rename to Expression.
        public Op op;

        public UnaryExp(Op op, Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            this.op = op;
            e = exp;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitUnary(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitUnary(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(OpToString(op));
            e.Write(writer);
        }
    }
}