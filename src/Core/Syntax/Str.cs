using System.IO;

namespace Pytocs.Core.Syntax
{
    /// <summary>
    /// Python string literal.
    /// </summary>
    public class Str : Exp
    {
        public readonly string s;
        public bool Format; // true if this is a format string.
        public bool Long;
        public bool Raw;
        public bool Unicode;

        public Str(string str, string filename, int start, int end) : base(filename, start, end)
        {
            s = str;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitStr(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitStr(this);
        }

        public override void Write(TextWriter writer)
        {
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