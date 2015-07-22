using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodePreTestLoopStatement : CodeStatement
    {
        public CodePreTestLoopStatement()
        {
            Body = new List<CodeStatement>();
        }

        public CodeExpression Test { get; set; }
        public List<CodeStatement> Body { get; private set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitPreTestLoop(this);
        }
    }
}
