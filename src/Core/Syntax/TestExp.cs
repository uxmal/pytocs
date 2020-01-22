using System.IO;

namespace Pytocs.Core.Syntax
{
    public class TestExp : Exp
    {
        public Exp Alternative;
        public Exp Condition;
        public Exp Consequent;

        public TestExp(string filename, int start, int end)
            : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitTest(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitTest(this);
        }

        public override void Write(TextWriter writer)
        {
            Consequent.Write(writer);
            writer.Write(" ");
            writer.Write("if");
            writer.Write(" ");
            Condition.Write(writer);
            writer.Write(" ");
            writer.Write("else");
            writer.Write(" ");
            Alternative.Write(writer);
        }
    }
}