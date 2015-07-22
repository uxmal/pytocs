using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class Decorator : Node
    {
        public DottedName className;
        public List<Argument> arguments;

        public Decorator(DottedName dn, List<Argument> args, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.className = dn;
            this.arguments = args;
        }
    }
}
