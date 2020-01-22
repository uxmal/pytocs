#region License

//  Copyright 2015-2020 John Källén
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

#endregion License

using Pytocs.Core;
using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using Pytocs.Core.TypeInference;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pytocs.Cli
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            FileSystem fs = new FileSystem();
            ConsoleLogger logger = new ConsoleLogger();
            if (args.Length == 0)
            {
                Translator xlator = new Translator("", "module_name", fs, logger);
                xlator.Translate("-", Console.In, Console.Out);
                Console.Out.Flush();
                return;
            }

            Dictionary<string, object> options = new Dictionary<string, object>();
            AnalyzerImpl typeAnalysis = new AnalyzerImpl(fs, logger, options, DateTime.Now);
            if (args[0].ToLower() == "-r")
            {
                string startDir = args.Length == 2
                    ? args[1]
                    : Directory.GetCurrentDirectory();
                typeAnalysis.Analyze(startDir);
                typeAnalysis.Finish();
                TypeReferenceTranslator types = new TypeReferenceTranslator(typeAnalysis.BuildTypeDictionary());
                //Console.WriteLine($"== Type dictionary: {types.Count}");
                //foreach (var de in types.OrderBy(d => d.Key.ToString()))
                //{
                //    Console.WriteLine("{0}: {1} {2}", de.Key, de.Key.Start, de.Value);
                //}

                DirectoryWalker walker = new DirectoryWalker(fs, startDir, "*.py");
                walker.Enumerate(state =>
                {
                    foreach (string file in fs.GetFiles(state.DirectoryName, "*.py", SearchOption.TopDirectoryOnly))
                    {
                        string path = fs.GetFullPath(file);
                        Translator xlator = new Translator(
                            state.Namespace,
                            fs.GetFileNameWithoutExtension(file),
                            fs,
                            logger);
                        Module module = typeAnalysis.GetAstForFile(path);
                        xlator.TranslateModuleStatements(
                            module.body.stmts,
                            types,
                            Path.ChangeExtension(path, ".py.cs"));
                    }
                });
            }
            else
            {
                foreach (string file in args)
                {
                    typeAnalysis.LoadFileRecursive(file);
                }

                typeAnalysis.Finish();
                TypeReferenceTranslator types = new TypeReferenceTranslator(
                    typeAnalysis.BuildTypeDictionary());
                foreach (string file in args)
                {
                    string path = fs.GetFullPath(file);
                    Translator xlator = new Translator(
                        "",
                        fs.GetFileNameWithoutExtension(file),
                        fs,
                        logger);
                    Module module = typeAnalysis.GetAstForFile(path);
                    xlator.TranslateModuleStatements(
                        module.body.stmts,
                        types,
                        Path.ChangeExtension(path, ".py.cs"));
                }
            }
        }
    }
}