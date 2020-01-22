using System.IO;

namespace Pytocs.Core.Syntax
{
    public class Identifier : Exp
    {
        public Identifier(string name, string filename, int start, int end) : base(filename, start, end)
        {
            Name = name;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitIdentifier(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitIdentifier(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(Name);
        }
    }
}