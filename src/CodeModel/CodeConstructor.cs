using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeConstructor : CodeMemberMethod
    {
        public CodeConstructor()
        {
            this.BaseConstructorArgs = new List<CodeExpression>();
            this.ChainedConstructorArgs = new List<CodeExpression>();
        }

        public List<CodeExpression> BaseConstructorArgs { get; private set; }
        public List<CodeExpression> ChainedConstructorArgs { get; private set; }

        public override T Accept<T>(ICodeMemberVisitor<T> visitor)
        {
            return visitor.VisitConstructor(this);
        }
    }
}
