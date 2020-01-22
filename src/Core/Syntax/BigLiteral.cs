using System.IO;
using System.Numerics;

namespace Pytocs.Core.Syntax
{
    public class BigLiteral : Exp
    {
        public BigLiteral(string value, BigInteger p, string filename, int start, int end) : base(filename, start, end)
        {
            Value = value;
            NumericValue = p;
        }

        public string Value { get; }
        public BigInteger NumericValue { get; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBigLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBigLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{0}", Value);
        }
    }
}