using System.IO;

namespace Pytocs.Core.Syntax
{
    public class RealLiteral : Exp
    {
        public readonly double NumericValue;

        public readonly string Value;

        public RealLiteral(string value, double p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitRealLiteral(this);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitRealLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            if (NumericValue == double.PositiveInfinity)
            {
                writer.Write("float('+inf')");
            }
            else if (NumericValue == double.NegativeInfinity)
            {
                writer.Write("float('-inf')");
            }
            else
            {
                writer.Write(Value);
            }
        }
    }
}