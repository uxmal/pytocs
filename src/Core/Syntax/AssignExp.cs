using System.IO;

namespace Pytocs.Core.Syntax
{
    public class AssignExp : Exp
    {
        public readonly Exp Annotation;
        public readonly Exp Dst;
        public readonly Op op;
        public readonly Exp Src;

        public AssignExp(Exp lhs, Op op, Exp rhs, string filename, int start, int end)
            : base(filename, start, end)
        {
            Dst = lhs;
            this.op = op;
            Src = rhs;
            Annotation = null;
        }

        public AssignExp(Exp lhs, Exp annotation, Op op, Exp rhs, string filename, int start, int end)
            : base(filename, start, end)
        {
            Dst = lhs;
            this.op = op;
            Src = rhs;
            Annotation = annotation;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitAssignExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitAssignExp(this);
        }

        public override void Write(TextWriter writer)
        {
            Dst.Write(writer);
            if (Annotation != null)
            {
                writer.Write(": ");
                Annotation.Write(writer);
            }
            writer.Write(OpToString(op));
            Src.Write(writer);
        }
    }
}