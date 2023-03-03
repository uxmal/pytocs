﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TorchSharp.Data;

namespace TorchCs
{
    public class Program
    {
        private const string usage =
@"Usage:
  TorchCs [options]

Options:
  -d, --dir               Convert all files in the directory 
  -n, --netstandard       Generate netstandard.cs file, The parameter has -d or -dir is valid. 
";
        static void Main(string[] args)
        {
            var options = ParseOptions(args);
            if (options.Count == 0) {
                Console.WriteLine(usage);
                return;
            }
            if (options.ContainsKey("--dir")) {
                Console.WriteLine("Conversion directory:" + options["--dir"].ToString());
                TorchUtil.ReplaceFolder(options["--dir"].ToString(), options.ContainsKey("--netstandard"));
            } else {
                foreach (var item in (List<string>)options["<files>"]) {
                    Console.WriteLine("Conversion file:" + item);
                    TorchUtil.ReplaceFile(item);
                }
            }
            if (options.ContainsKey("--netstandard")) {
                if (options.ContainsKey("--dir")) {
                    Console.WriteLine("Generate netstandard.cs file");
                    TorchUtil.CreateNetstandardCode(Path.GetDirectoryName(options["--dir"].ToString()));
                }
            }
            Console.WriteLine("Conversion completed!");
        }

        private static IDictionary<string, object> ParseOptions(string[] args)
        {
            var result = new Dictionary<string, object>();
            var files = new List<string>();

            int index = 0;
            while (index < args.Length) {
                var arg = args[index++];
                if (!arg.StartsWith('-')) {
                    files.Add(arg);

                } else if (arg == "-d" || arg == "--dir") {
                    result["--dir"] = args[index++];
                } else if (arg == "-n" || arg == "--netstandard") {
                    result["--netstandard"] = true;
                }
            }
            result["<files>"] = files;
            return result;
        }


    }
}
