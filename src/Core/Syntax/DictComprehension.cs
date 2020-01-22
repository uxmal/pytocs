namespace Pytocs.Core.Syntax
{
    public class DictComprehension : Exp
    {
        public Exp key;
        public CompFor source;
        public Exp value;

        public DictComprehension(Exp key, Exp value, CompFor collection, string filename, int start, int end) : base(filename, start, end)
        {
            this.key = key;
            this.value = value;
            source = collection;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitDictComprehension(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitDictComprehension(this);
        }
    }
}