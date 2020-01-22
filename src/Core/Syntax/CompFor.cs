using System.IO;

namespace Pytocs.Core.Syntax
{
    public class CompFor : CompIter
    {
        public Exp collection;
        public Exp variable;

        public CompFor(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public bool Async { get; set; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitCompFor(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitCompFor(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("for");
            writer.Write(" ");
            variable.Write(writer);
            writer.Write(" ");
            writer.Write("in");
            writer.Write(" ");
            collection.Write(writer);
            if (next != null)
            {
                writer.Write(" ");
                next.Write(writer);
            }
        }
    }
}