using System.IO;

namespace Pytocs.Core.Syntax
{
    public class SetComprehension : Exp
    {
        public Exp Collection;
        public Exp Projection;

        public SetComprehension(Exp proj, Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            Projection = proj;
            Collection = coll;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitSetComprehension(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitSetComprehension(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{");
            Projection.Write(writer);
            writer.Write(" ");
            Collection.Write(writer);
            writer.Write("}");
        }
    }
}