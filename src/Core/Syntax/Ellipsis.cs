namespace Pytocs.Core.Syntax
{
    public class Ellipsis : Exp
    {
        public Ellipsis(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitEllipsis(this);
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitEllipsis(this);
        }
    }
}