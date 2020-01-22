using System.IO;

namespace Pytocs.Core.Syntax
{
    public class StarExp : Exp
    {
        public Exp e;

        public StarExp(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitStarExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitStarExp(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("*");
            e.Write(writer);
        }
    }
}