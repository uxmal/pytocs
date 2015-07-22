using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public abstract class CodeMember 
    {
        public CodeMember()
        {
            this.CustomAttributes = new List<CodeAttributeDeclaration>();
        }

        public string Name { get; set; }
        public List<CodeAttributeDeclaration> CustomAttributes { get; private set; }
        public MemberAttributes Attributes { get; set; }

        public abstract T Accept<T>(ICodeMemberVisitor<T> visitor);
    }
}
