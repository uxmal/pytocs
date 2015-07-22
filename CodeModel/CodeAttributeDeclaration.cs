using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeAttributeDeclaration
    {
        public CodeTypeReference AttributeType { get; set; }
        public List<CodeAttributeArgument> Arguments { get; set; }
    }
}
