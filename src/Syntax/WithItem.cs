using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class WithItem : Node
    {
        public Exp t;
        public Exp e;

        public WithItem(Exp t, Exp e, string filename, int start, int end) : base(filename, start, end)
        {
            this.t = t;
            this.e = e;
        }
    }
}
