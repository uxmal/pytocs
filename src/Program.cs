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
using Pytocs.TypeInference;

namespace Pytocs
{
    class Program
    {
        static void Main(string[] args)
        {
            var fs = new FileSystem();
            var logger = new ConsoleLogger();
            if (args.Length == 0)
            {
                var xlator = new Translator("", "module_name", fs, logger);
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
#if READY_FOR_TYPES
                var typeAnalysis = new Pytocs.TypeInference.AnalyzerImpl(fs, logger, new Dictionary<string, object>(), DateTime.Now);
                typeAnalysis.Analyze(".");
                TranslateModules(typeAnalysis);
#else
                var walker = new DirectoryWalker("*.py");
                walker.Enumerate();
#endif
            }
            else
            {
                foreach (var fileName in args)
                {
                    var xlator = new Translator(
                        "", 
                        Path.GetFileNameWithoutExtension(fileName),
                        new FileSystem(),
                        new ConsoleLogger());
                    xlator.TranslateFile(fileName, fileName + ".cs");
                }
            }
        }

        private static void TranslateModules(Analyzer typeAnalysis)
        {
            var bind = typeAnalysis.GetModuleBindings().ToArray();
            throw new NotImplementedException();
        }
    }
}
