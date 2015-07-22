using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                var xlator = new Translator("", "module_name");
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
                var symtab = new SymbolTable(); 
                walker.Enumerate();
            }
            else
            {
                var symtab = new SymbolTable(); 
                foreach (var fileName in args)
                {
                    //ProcessFile(fileName, symtab);
                }
            }
        }
    }
}
