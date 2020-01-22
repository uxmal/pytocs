using System.IO;

namespace Pytocs.Core.Syntax
{
    public class YieldExp : Exp
    {
        public readonly Exp exp;

        public YieldExp(Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            this.exp = exp;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitYieldExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitYieldExp(this);
        }

        public override void Write(TextWriter writer)
        {
            exp.Write(writer);
        }
    }
}