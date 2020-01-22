using System.IO;

namespace Pytocs.Core.Syntax
{
    public class YieldFromExp : Exp
    {
        public readonly Exp Expression;

        public YieldFromExp(Exp exp, string filename, int start, int end) : base(filename, start, end)
        {
            Expression = exp;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitYieldFromExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitYieldFromExp(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("from");
            writer.Write(" ");
            Expression.Write(writer);
        }
    }
}