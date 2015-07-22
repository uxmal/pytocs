using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeCatchClause
    {
        public CodeCatchClause()
        {
            Statements = new List<CodeStatement>();
        }

        public CodeCatchClause(string localName) : this()
        {
            LocalName = localName;
        }

        public CodeCatchClause(string localName, CodeTypeReference catchExceptionType) : this()
        {
            LocalName = localName;
            CatchExceptionType = catchExceptionType;
        }

        public CodeCatchClause(string localName, CodeTypeReference catchExceptionType, params CodeStatement[] statements): this()
        {
            LocalName = localName;
            CatchExceptionType = catchExceptionType;
            Statements.AddRange(statements);
        }

        public CodeTypeReference CatchExceptionType { get; set; }
        public string LocalName { get; set; }
        public List<CodeStatement> Statements { get; private set; }
    }
}
