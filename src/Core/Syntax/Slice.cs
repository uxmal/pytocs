using System.IO;

namespace Pytocs.Core.Syntax
{
    public class Slice : Exp
    {
        public Exp lower;
        public Exp step;
        public Exp upper;

        public Slice(Exp start, Exp end, Exp slice, string filename, int s, int e) : base(filename, s, e)
        {
            lower = start;
            step = end;
            upper = slice;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitSlice(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitSlice(this);
        }

        public override void Write(TextWriter writer)
        {
            if (lower == null && step == null && upper == null)
            {
                writer.Write("::");
            }
            else if (lower != null)
            {
                lower.Write(writer);
                if (step != null)
                {
                    writer.Write(':');
                    step.Write(writer);
                    writer.Write(':');
                    if (upper != null)
                    {
                        writer.Write(':');
                        upper.Write(writer);
                    }
                }
            }
        }
    }
}