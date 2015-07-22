using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class AliasedName : Node
    {
        public readonly DottedName orig;
        public readonly Identifier alias;

        public AliasedName(Identifier orig, Identifier alias, string filename, int start, int end) : base(filename, start, end)
        {
            this.orig = new DottedName(new List<Identifier> {orig }, filename, start, end);
            this.alias = alias;
        }

        public AliasedName(DottedName orig, Identifier alias, string filename, int start, int end) : base(filename, start, end)
        {
            this.orig = orig;
            this.alias = alias;
        }
    }
}
