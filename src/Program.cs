using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.TypeInference;

namespace Pytocs
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                var xlator = new Translator("", "module_name", 
                    new ConsoleLogger());
                xlator.Translate("-", Console.In, Console.Out);
                Console.Out.Flush();
                return;
            }

            if (args[0].ToLower() == "-d")
            {
   //             org.yinwang.pysonar.demos.Demo.DemoMain(args);
                return;
            }

            if (args[0].ToLower() == "-r")
            {
                var walker = new DirectoryWalker("*.py");
                walker.Enumerate();
            }
            else
            {
                foreach (var fileName in args)
                {
                    var xlator = new Translator(
                        "", 
                        Path.GetFileNameWithoutExtension(fileName),
                        new ConsoleLogger());
                    xlator.TranslateFile(fileName, fileName + ".cs");
                }
            }
        }
    }
}
