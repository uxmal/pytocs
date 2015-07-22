using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CodeCommentStatement :CodeStatement
    {
        public CodeCommentStatement(string comment)
        {
            this.Comment = comment;
        }

        public string Comment { get; set; }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitComment(this);
        }
    }
}
