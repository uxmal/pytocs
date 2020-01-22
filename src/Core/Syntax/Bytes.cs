using System.IO;

namespace Pytocs.Core.Syntax
{
    public class Bytes : Exp
    {
        public readonly string s;
        public bool Raw;

        public Bytes(string str, string filename, int start, int end)
            : base(filename, start, end)
        {
            s = str;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBytes(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBytes(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("b");
            if (Raw)
            {
                writer.Write("r");
            }

            writer.Write('\"');
            writer.Write(s);
            writer.Write('\"');
        }
    }
}