namespace Pytocs.Core.Syntax
{
    public class GeneratorExp : Exp
    {
        public Exp Collection;
        public Exp Projection;

        public GeneratorExp(Exp proj, Exp coll, string filename, int start, int end) : base(filename, start, end)
        {
            Projection = proj;
            Collection = coll;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitGeneratorExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitGeneratorExp(this);
        }
    }
}