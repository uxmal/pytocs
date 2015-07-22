using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeObjectCreateExpression : CodeExpression
    {
        public CodeObjectCreateExpression()
        {
            this.Arguments = new List<CodeExpression>();
            this.Initializers = new List<CodeExpression>();
        }

        public CodeTypeReference Type { get; set; }
        public CodeInitializerExpression Initializer { get; set; }
        public List<CodeExpression> Arguments { get; private set; }
        public List<CodeExpression> Initializers { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitObjectCreation(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitObjectCreation(this);
        }
    }
}
