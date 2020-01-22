using System.IO;

namespace Pytocs.Core.Syntax
{
    public class IterableUnpacker : Exp
    {
        public readonly Exp Iterable;

        public IterableUnpacker(Exp iterable, string filename, int start, int end) : base(filename, start, end)
        {
            Iterable = iterable;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitIterableUnpacker(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitIterableUnpacker(this);
        }

        public override void Write(TextWriter w)
        {
            w.Write("*");
            Iterable.Write(w);
        }
    }
}