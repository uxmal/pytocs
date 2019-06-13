using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Runtime
{
    public class DictionaryUtils
    {
        public static Dictionary<K, V> Unpack<K, V>(params object [] items)
        {
            var result = new Dictionary<K, V>();
            foreach (var oItem in items)
            {
                if (oItem is ITuple tuple)
                {
                    result[(K)tuple[0]] = (V)tuple[1];
                }
                else if (oItem is IDictionary dict)
                {
                    foreach (DictionaryEntry de in dict)
                    {
                        result[(K)de.Key] = (V)de.Value;
                    }
                }
            }
            return result;
        }
    }
}
