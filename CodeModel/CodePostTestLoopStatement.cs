using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodePostTestLoopStatement : CodeStatement
    {
        public CodePostTestLoopStatement()
        {
            Body = new List<CodeStatement>();
        }

        public List<CodeStatement> Body { get; private set; }
        public CodeExpression Test { get; set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitPostTestLoop(this);
        }
    }
}
