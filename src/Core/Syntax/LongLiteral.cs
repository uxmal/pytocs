using System.IO;

namespace Pytocs.Core.Syntax
{
    public class LongLiteral : Exp
    {
        public readonly long NumericValue;
        public readonly string Value;

        public LongLiteral(string value, long p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitLongLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitLongLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{0}L", Value);
        }
    }
}