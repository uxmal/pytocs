using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public abstract class CodeStatement
    {
        public abstract T Accept<T>(ICodeStatementVisitor<T> visitor);
    }
}
