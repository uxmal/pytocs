using System.IO;

namespace Pytocs.Core.Syntax
{
    public class BinExp : Exp
    {
        public Exp l;
        public Op op;
        public Exp r;

        public BinExp(Op op, Exp l, Exp r, string filename, int start, int end) : base(filename, start, end)
        {
            this.op = op; this.l = l; this.r = r;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBinExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBinExp(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("(");
            l.Write(writer);
            writer.Write(" {0} ", OpToString(op));
            r.Write(writer);
            writer.Write(")");
        }
    }
}