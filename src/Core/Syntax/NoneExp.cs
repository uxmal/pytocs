using System.IO;

namespace Pytocs.Core.Syntax
{
    public class NoneExp : Exp
    {
        public NoneExp(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitNoneExp();
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitNoneExp();
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("None");
        }
    }
}