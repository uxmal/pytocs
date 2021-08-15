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
using System.Collections.Generic;
using System.Text;

namespace Pytocs.Core.TypeInference
{
    public class Statistics
    {
        private readonly IDictionary<string, long> contents = new Dictionary<string, long>();

        public void PutInt(string key, long value)
        {
            contents[key] = value;
        }

        public void Inc(string key, long x)
        {
            long old = GetInt(key);
            contents[key] = old + x;
        }

        public void Inc(string key)
        {
            Inc(key, 1);
        }

        public long GetInt(string key)
        {
            if (!contents.TryGetValue(key, out long ret))
                return 0;
            return ret;
        }

        public string Print()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var e in contents)
            {
                sb.AppendLine();
                sb.AppendFormat("- {0}: {1}", e.Key, e.Value);
            }
            return sb.ToString();
        }
    }
}