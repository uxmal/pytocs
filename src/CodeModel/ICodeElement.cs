using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public interface ICodeElement
    {
        T Accept<T>(ICodeElementVisitor<T> visitor);
    }
}
