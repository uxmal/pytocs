using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class PySet : Exp
    {
        public List<Exp> exps;

        public PySet(List<Exp> exps, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.exps = exps;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitSet(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitSet(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{ ");
            string sep = "";
            foreach (Exp item in exps)
            {
                writer.Write(sep);
                sep = ", ";
                item.Write(writer);
            }
            writer.Write(" }");
        }
    }
}