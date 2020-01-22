using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class ExpList : Exp
    {
        public readonly List<Exp> Expressions;

        public ExpList(List<Exp> exps, string filename, int start, int end) : base(filename, start, end)
        {
            Expressions = exps;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitExpList(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitExpList(this);
        }

        public override IEnumerable<Exp> AsList()
        {
            return Expressions;
        }

        public override void Write(TextWriter writer)
        {
            string sep = "";
            foreach (Exp exp in Expressions)
            {
                writer.Write(sep);
                sep = ",";
                exp.Write(writer);
            }
        }
    }
}