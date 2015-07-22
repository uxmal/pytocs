using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class ClassDef : Statement
    {
        public readonly Identifier name;
        public readonly List<Exp> args;    //$REVIEW: could these be dotted names?
        public readonly SuiteStatement body;

        public ClassDef(Identifier name, List<Exp> baseClasses, SuiteStatement body, string filename, int start, int end) : base(filename, start, end) 
        {
            this.name = name;
            this.args = baseClasses;
            this.body = body;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitClass(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitClass(this);
        }
    }
}
