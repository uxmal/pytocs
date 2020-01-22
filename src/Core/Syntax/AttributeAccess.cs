using System.IO;

namespace Pytocs.Core.Syntax
{
    public class AttributeAccess : Exp
    {
        public readonly Exp Expression;
        public readonly Identifier FieldName;

        public AttributeAccess(Exp expr, Identifier fieldName, string filename, int start, int end) : base(filename, start, end)
        {
            Expression = expr;
            FieldName = fieldName;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitFieldAccess(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitFieldAccess(this);
        }

        public override void Write(TextWriter w)
        {
            Expression.Write(w);
            w.Write(".{0}", FieldName);
        }
    }
}