#region License
//  Copyright 2015-2022 John Källén
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

using Pytocs.Core;
using Pytocs.Core.Translate;
using Pytocs.Core.TypeInference;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Pytocs.Cli
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
            if (args[0] == "-v")
            {
                WriteVersion();
                return;
            }

            var options = new Dictionary<string, object>();
            var typeAnalysis = new AnalyzerImpl(fs, logger, options, DateTime.Now);
            if (args[0].ToLower() == "-r")
            {
                var startDir = args.Length == 2
                    ? args[1]
                    : Directory.GetCurrentDirectory();
                typeAnalysis.Analyze(startDir);
                typeAnalysis.Finish();
                var types = new TypeReferenceTranslator(typeAnalysis.BuildTypeDictionary());
                //Console.WriteLine($"== Type dictionary: {types.Count}");
                //foreach (var de in types.OrderBy(d => d.Key.ToString()))
                //{
                //    Console.WriteLine("{0}: {1} {2}", de.Key, de.Key.Start, de.Value);
                //}

                var walker = new DirectoryWalker(fs, startDir, "*.py");
                walker.Enumerate(state =>
                {
                    foreach (var file in fs.GetFiles(state.DirectoryName, "*.py", SearchOption.TopDirectoryOnly))
                    {
                        var path = fs.GetFullPath(file);
                        var xlator = new Translator(
                             state.Namespace,
                             fs.GetFileNameWithoutExtension(file),
                             fs,
                             logger);
                        var module = typeAnalysis.GetAstForFile(path);
                        if (module is null)
                        {
                            logger.Error("Unable to load {0}.", path);
                            continue;
                        }
                        xlator.TranslateModuleStatements(
                            module.Body.Statements,
                            types,
                            Path.ChangeExtension(path, ".py.cs"));
                    }
                });
            }
            else
            {
                foreach (var file in args)
                {
                    typeAnalysis.LoadFileRecursive(file);
                }
                typeAnalysis.Finish();
                var types = new TypeReferenceTranslator(
                    typeAnalysis.BuildTypeDictionary());
                foreach (var file in args)
                {
                    var path = fs.GetFullPath(file);
                    var xlator = new Translator(
                        "",
                         fs.GetFileNameWithoutExtension(file),
                        fs,
                         logger);
                    var module = typeAnalysis.GetAstForFile(path);
                    if (module is null)
                    {
                        logger.Error("Unable to load {0}.", path);
                        continue;
                    }
                    xlator.TranslateModuleStatements(
                        module.Body.Statements,
                        types,
                        Path.ChangeExtension(path, ".py.cs"));
                }
            }
        }

        private static void WriteVersion()
        {
            var x = typeof(Program).Assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
                .Cast<AssemblyFileVersionAttribute>()
                .FirstOrDefault();
            if (x is null)
            {
                Console.WriteLine("Unknown version");
            }
            else
            {
                Console.WriteLine("Pytocs command line tool, version {0}", x.Version);
            }
        }
    }
}
