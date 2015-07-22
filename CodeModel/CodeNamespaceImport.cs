using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeNamespaceImport
    {
        public CodeNamespaceImport(string @namespace)
        {
            this.Namespace = @namespace;
        }

        public string Namespace { get; set; }
    }
}
