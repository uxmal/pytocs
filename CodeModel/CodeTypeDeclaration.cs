using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeTypeDeclaration : CodeMember
    {
        public CodeTypeDeclaration(string name) : this()
        {
            this.Name = name;
        }

        public CodeTypeDeclaration()
        {
            this.Members = new List<CodeMember>();
            this.BaseTypes = new List<CodeTypeReference>();
            this.Comments = new List<CodeCommentStatement>();
            Attributes = MemberAttributes.Public;
        }

        public bool IsClass { get; set; }

        public List<CodeMember> Members { get; private set; }

        public List<CodeTypeReference> BaseTypes { get; private set; }

        public List<CodeCommentStatement> Comments { get; private set; }

        public override T Accept<T>(ICodeMemberVisitor<T> visitor)
        {
            return visitor.VisitTypeDefinition(this);
        }

    }
}
