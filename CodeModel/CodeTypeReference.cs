using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeTypeReference
    {
        public CodeTypeReference()
        {
            TypeArguments = new List<CodeTypeReference>();
        }

        public CodeTypeReference(Type type)
            : this()
        {
            TypeName = type.FullName;
        }

        public CodeTypeReference(string typeName)
            : this()
        {
            TypeName = typeName;
        }

        public CodeTypeReference(string typeName, params CodeTypeReference[] typeArgs)
            : this()
        {
            this.TypeName = typeName;
            this.TypeArguments.AddRange(typeArgs);
        }

        public string TypeName { get; set; }
        public List<CodeTypeReference> TypeArguments { get; private set; }
    }
}
