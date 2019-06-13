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
                // We could have used ITuple here, but that interface 
                // is only supported in .NET Standard 2.1, which at the 
                // time of writing hadn't been released.

                var t = oItem.GetType();
                var kf = t.GetField("Item1");
                var vf = t.GetField("Item2");
                if (kf != null && vf != null)
                {
                    var key = (K)kf.GetValue(oItem);
                    var value = (V)vf.GetValue(oItem);
                    result[key] = value;
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
