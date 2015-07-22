using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    [Flags]
    public enum MemberAttributes
    {
        Abstract = 1,
        Final = 2,
        Static = 3,
        Override = 4,
        Const = 5,
        ScopeMask = 15,
        
        New = 16,
        VTableMask = 240,
        Overloaded = 256,
        Assembly = 4096,
        FamilyAndAssembly = 8192,
        Family = 12288,
        FamilyOrAssembly = 16384,
        
        Private = 20480,
        Public = 24576,
        AccessMask = 61440,
    }
}
