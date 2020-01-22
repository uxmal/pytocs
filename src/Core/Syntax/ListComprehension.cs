using System.IO;

namespace Pytocs.Core.Syntax
{
    public class ListComprehension : Exp
    {
        public Exp Collection;
        public Exp Projection;

        public ListComprehension(Exp proj, Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            Projection = proj;
            Collection = coll;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitListComprehension(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitListComprehension(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("[");
            Projection.Write(writer);
            writer.Write(" ");
            Collection.Write(writer);
            writer.Write("]");
        }
    }
}