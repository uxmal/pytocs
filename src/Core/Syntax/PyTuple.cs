using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class PyTuple : Exp
    {
        public List<Exp> values;

        public PyTuple(List<Exp> values, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.values = values;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitTuple(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitTuple(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("(");
            string sep = "";
            foreach (Exp e in values)
            {
                writer.Write(sep);
                sep = ",";
                e.Write(writer);
            }
            writer.Write(")");
        }
    }
}