using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeApplicationExpression : CodeExpression
    {
        public CodeExpression Method { get; private set; }

        private  CodeApplicationExpression()
        {
            this.Arguments = new List<CodeExpression>();
        }

        public CodeApplicationExpression(CodeExpression fn, IEnumerable<CodeExpression> args) : this()
        {
            this.Method = fn;
            this.Arguments.AddRange(args);
        }

        public List<CodeExpression> Arguments { get; private set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitApplication(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitApplication(this);
        }
    }
}
