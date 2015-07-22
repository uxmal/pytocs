using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeNamespace :ICodeElement
    {
        public CodeNamespace()
        {
            this.Types = new List<CodeTypeDeclaration>();
            this.Imports = new List<CodeNamespaceImport>();
            this.Comments = new List<CodeCommentStatement>();
        }

        public CodeNamespace(string @namespace) : this()
        {
            this.Name = @namespace;
        }

        public T Accept<T>(ICodeElementVisitor<T> visitor)
        {
            return visitor.VisitNamespace(this);
        }

        public string Name { get; set; }
        public List<CodeTypeDeclaration> Types {get; private set; }
        public List<CodeNamespaceImport> Imports { get; private set; }
        public List<CodeCommentStatement> Comments { get; private set; }
    }
}
