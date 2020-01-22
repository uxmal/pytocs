using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class PyList : Exp
    {
        public readonly List<Exp> elts;

        public PyList(List<Exp> elts, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.elts = elts;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitList(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitList(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("[");
            string sep = "";
            foreach (Exp exp in elts)
            {
                writer.Write(sep);
                sep = ",";
                exp.Write(writer);
            }
            writer.Write("]");
        }
    }
}