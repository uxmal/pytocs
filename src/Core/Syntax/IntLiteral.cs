using System.IO;

namespace Pytocs.Core.Syntax
{
    public class IntLiteral : Exp
    {
        public readonly long NumericValue;
        public readonly string Value;

        public IntLiteral(string value, long p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitIntLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitIntLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Value);
        }
    }
}