using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Syntax
{
    public class Module : Node
    {
        public SuiteStatement body;

        public Module(string moduleName, SuiteStatement body, string filename, int begin, int end) : base(filename, begin, end) {
            this.Name = moduleName;
            this.body = body;
        }

        public override string ToString()
        {
            return "(module:" + Filename + ")";
        }
    }
}
