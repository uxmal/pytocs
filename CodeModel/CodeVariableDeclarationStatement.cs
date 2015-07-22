using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeVariableDeclarationStatement : CodeStatement
    {
        public CodeVariableDeclarationStatement(string typeName, string name)
        {
            this.Type = new CodeTypeReference(typeName);
            this.Name = name;
        }

        public CodeTypeReference Type { get; set; }
        public string Name { get; set; }
        public CodeExpression InitExpression { get; set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitVariableDeclaration(this);
        }
    }
}
