using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CodeMemberField : CodeMember
    {
        public CodeTypeReference FieldType { get; set; }
        public string FieldName { get; set; }
        public CodeExpression InitExpression;

        public CodeMemberField(Type type, string fieldName)
        {
            this.FieldType = new CodeTypeReference(type);
            this.FieldName = fieldName;
        }

        public override T Accept<T>(ICodeMemberVisitor<T> visitor)
        {
            return visitor.VisitField(this);
        }
    }
}
