using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class FunctionDef : Statement
    {
        public readonly Identifier name;
        public readonly List<Parameter> parameters;
        public readonly Exp annotation;
        public readonly SuiteStatement body;
        public bool called = false;         //$ move to big state
        public readonly Identifier vararg;
        public readonly Identifier kwarg;

        public FunctionDef(
            Identifier name, 
            List<Parameter> parameters,
            Identifier vararg,
            Identifier kwarg,
            Exp annotation,
            SuiteStatement body, string filename, int start, int end) 
            : base(filename, start, end) 
        {
            this.name = name;
            this.parameters = parameters;
            this.vararg = vararg;
            this.kwarg = kwarg;
            this.annotation = annotation;
            this.body = body;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitFuncdef(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitFunctionDef(this);
        }
    }

    public class Lambda : Exp
    {
        public List<VarArg> args;
        public Exp body;

        public Lambda(string filename, int start, int end) : base(filename, start, end) { }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitLambda(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitLambda(this);
        }
    }

}
