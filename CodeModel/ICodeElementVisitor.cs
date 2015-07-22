using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public interface ICodeElementVisitor<T>
    {
        T VisitNamespace(CodeNamespace n);
        T VisitStatement(CodeStatement bin);
    }
}
