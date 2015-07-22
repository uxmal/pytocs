using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class ExceptHandler : Node
    {
        public Exp type;
        public Identifier name;
        public SuiteStatement body;

        public ExceptHandler(Exp type, Identifier name, SuiteStatement body, string filename, int start, int end) : base(filename, start, end)
        {
            this.type = type;
            this.name = name;
            this.body = body;
        }
    }
}
