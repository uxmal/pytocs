using System;
using System.Collections.Generic;
using System.Text;

namespace Pytocs.TypeInference
{
    public class Statistics
    {
        IDictionary<string, long> contents = new Dictionary<string, long>();

        public void putInt(string key, long value)
        {
            contents[key] = value;
        }

        public void inc(string key, long x)
        {
            long old = getInt(key);
            contents[key] = old + x;
        }

        public void inc(string key)
        {
            inc(key, 1);
        }

        public long getInt(string key)
        {
            long ret;
            if (!contents.TryGetValue(key, out ret))
                return 0;
            return ret;
        }

        public string print()
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