using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeMemberMethod : CodeMember
    {
        public CodeMemberMethod()
        {
            this.Parameters = new List<CodeParameterDeclarationExpression>();
            this.Statements = new List<CodeStatement>();
            this.Comments = new List<CodeCommentStatement>();
        }

        public List<CodeParameterDeclarationExpression> Parameters { get; private set; }
        public CodeTypeReference ReturnType { get; set; }

        public List<CodeStatement> Statements { get; private set; }
        public List<CodeCommentStatement> Comments { get; private set; }

        public override T Accept<T>(ICodeMemberVisitor<T> visitor)
        {
            return visitor.VisitMethod(this);
        }
    }
}
