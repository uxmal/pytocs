using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeCompileUnit
    {
        public CodeCompileUnit()
        {
            this.Namespaces = new List<CodeNamespace>();
        }

        public List<CodeNamespace> Namespaces { get; private set; }
    }
}
