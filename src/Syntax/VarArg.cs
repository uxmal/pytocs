using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Syntax
{
    public class VarArg
    {
        public Identifier name;
        public Exp test;

        public static VarArg Keyword(string name) { return new VarArg(); }
    }
}
