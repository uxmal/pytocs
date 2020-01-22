using System;
using System.Collections.Generic;

namespace Pytocs.Core.Syntax
{
    public class FromStatement : Statement
    {
        public readonly List<AliasedName> AliasedNames;
        public readonly DottedName DottedName;

        public FromStatement(DottedName name, List<AliasedName> aliasedNames, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            DottedName = name;
            AliasedNames = aliasedNames ?? throw new ArgumentNullException(nameof(aliasedNames));
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitFrom(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitFrom(this);
        }

        internal bool isImportStar()
        {
            return AliasedNames.Count == 0;
        }
    }
}