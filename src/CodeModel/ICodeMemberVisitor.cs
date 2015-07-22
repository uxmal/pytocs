using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public interface ICodeMemberVisitor<T>
    {
        T VisitField(CodeMemberField field);
        T VisitMethod(CodeMemberMethod method);
        T VisitTypeDefinition(CodeTypeDeclaration type);
        T VisitConstructor(CodeConstructor codeConstructor);
    }
}
