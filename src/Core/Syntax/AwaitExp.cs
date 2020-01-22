using System.IO;

namespace Pytocs.Core.Syntax
{
    public class AwaitExp : Exp
    {
        public readonly Exp exp;

        public AwaitExp(Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            this.exp = exp;
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitAwait(this);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitAwait(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("await");
            writer.Write(" ");
            exp.Write(writer);
        }
    }
}