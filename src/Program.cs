#region License
//  Copyright 2015 John Källén
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
