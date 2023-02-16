#region License
//  Copyright 2015-2021 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace pytocs.runtime
{
    public class DictionaryUtils
    {
        public static Dictionary<K, V> Unpack<K, V>(params object [] items)
            where K : notnull
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
                    var key = (K)kf.GetValue(oItem)!;
                    var value = (V)vf.GetValue(oItem)!;
                    result[key] = value;
                }
                else if (oItem is IDictionary dict)
                {
                    foreach (DictionaryEntry de in dict)
                    {
                        result[(K)de.Key] = (V)de.Value!;
                    }
                }
            }
            return result;
        }
    }
}
