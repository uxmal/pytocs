using System.IO;

namespace Pytocs.Core.Syntax
{
    public class ImaginaryLiteral : Exp
    {
        public ImaginaryLiteral(string value, double im, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = im;
        }

        public string Value { get; }
        public double NumericValue { get; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitImaginary(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitImaginaryLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Value);
            writer.Write("j");
        }
    }
}