using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public abstract class CodeExpression
    {
        public abstract T Accept<T>(ICodeExpressionVisitor<T> visitor);

        public abstract void Accept(ICodeExpressionVisitor visitor);
    }
}
