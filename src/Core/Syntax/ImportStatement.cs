using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class ImportStatement : Statement
    {
        public readonly List<AliasedName> names;

        public ImportStatement(List<AliasedName> names, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            // TODO: Complete member initialization
            this.names = names;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitImport(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitImport(this);
        }
    }
}