using System;
using System.Collections.Generic;
using System.Text;

namespace Pytocs.TypeInference
{
    public class Statistics
    {
        IDictionary<string, object> contents = new Dictionary<string, object>();

        public void putInt(string key, long value)
        {
            contents[key] = value;
        }


        public void inc(string key, long x)
        {
            long? old = getInt(key);
            if (!old.HasValue)
            {
                contents[key] = 1;
            }
            else
            {
                contents[key] = old.Value + x;
            }
        }

        public void inc(string key)
        {
            inc(key, 1);
        }

        public long getInt(string key)
        {
            object ret;
            if (!contents.TryGetValue(key, out ret))
                return 0;
            return (long) ret;
        }


        public string print()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var e in contents)
            {
                sb.AppendFormat("\n- {0}: {1}", e.Key, e.Value);
            }
            return sb.ToString();
        }

        internal void putDate(string key, DateTime dateTime)
        {
            contents[key] = dateTime;
        }

        internal DateTime getDateTime(string key)
        {
            object dt;
            if (contents.TryGetValue(key, out dt))
                return (DateTime) dt;
            return default(DateTime);
        }
    }
}