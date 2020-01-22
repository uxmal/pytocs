using System.IO;

namespace Pytocs.Core.Syntax
{
    public class CompIf : CompIter
    {
        public Exp test;

        public CompIf(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitCompIf(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitCompIf(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("if");
            writer.Write(" ");
            test.Write(writer);
            if (next != null)
            {
                writer.Write(" ");
                next.Write(writer);
            }
        }
    }
}