using System.IO;

namespace Pytocs.Core.Syntax
{
    public class BooleanLiteral : Exp
    {
        public readonly bool Value;

        public BooleanLiteral(bool b, string filename, int start, int end) : base(filename, start, end)
        {
            Value = b;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitBooleanLiteral(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitBooleanLiteral(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Value ? "True" : "False");
        }
    }
}