using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pytocs.Syntax
{
    public class AliasedExp : Exp
    {
        public Exp exp;
        public Identifier alias;

        public AliasedExp(Exp t, Identifier alias, string filename, int start, int end) : base(filename, start, end)
        {
            this.exp = t;
            this.alias = alias;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitAliasedExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitAliasedExp(this);
        }

        public override void Write(System.IO.TextWriter writer)
        {
            if (exp == null)
                return;
            writer.Write(alias != null ? " {0}, {1}" : "{0}", exp, alias);
        }
    }
}
