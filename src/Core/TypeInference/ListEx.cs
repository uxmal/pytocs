using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.TypeInference
{
    public static class ListEx
    {
        public static List<T> SubList<T>(this List<T> list, int iMin, int iMac)
        {
            return list.Skip(iMin).Take(iMac - iMin).ToList();
        }
    }
}