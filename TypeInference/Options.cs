using System;
using System.Collections.Generic;

namespace org.yinwang.pysonar
{

    public class Options
    {
        private Dictionary<string, object> optionsMap = new Dictionary<string, object>();
        private List<string> args = new List<string>();


        public Options(params string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i];
                if (key.StartsWith("--"))
                {
                    if (i + 1 >= args.Length)
                        throw new InvalidOperationException("option needs a value: " + key);
                    key = key.Substring(2);
                    string value = args[i + 1];
                    if (!value.StartsWith("-"))
                    {
                        optionsMap[key] = value;
                        i++;
                    }
                }
                else if (key.StartsWith("-"))
                {
                    key = key.Substring(1);
                    optionsMap[key] = true;
                }
                else
                {
                    this.args.Add(key);
                }
            }
        }

        public object get(string key)
        {
            return optionsMap[key];
        }

        public bool hasOption(string key)
        {
            object v = optionsMap[key];
            if (v is bool)
            {
                return (bool) v;
            }
            else
            {
                return false;
            }
        }

        public void put(string key, object value)
        {
            optionsMap[key] = value;
        }

        public List<string> getArgs()
        {
            return args;
        }

        public Dictionary<string, object> getOptionsMap()
        {
            return optionsMap;
        }


        public static void main(string[] args)
        {
            Options options = new Options(args);
            foreach (string key in options.optionsMap.Keys)
            {
                System.Console.WriteLine(key + " = " + options.get(key));
            }
        }
    }
}