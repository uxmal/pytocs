using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeArrayCreateExpression : CodeExpression
    {
        public CodeArrayCreateExpression(Type type, CodeExpression[] codeExpression)
        {
            this.ElementType = new CodeTypeReference(type);
            this.Initializers = codeExpression;
        }

        public CodeTypeReference ElementType { get; set; }
        public CodeExpression[] Initializers { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitArrayInitializer(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitArrayInitializer(this);
        }
    }
}
