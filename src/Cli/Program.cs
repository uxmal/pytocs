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
    public class Program
    {
        private const string usage = 
@"Usage:
  pytocs [options]

Options:
  -v, --version                 Print version.
  -r, --recursive DIRECTORY     Transpile all the files in the directory and
                                all its subdirectories [default: .]
  -p, --post-process PPLIST     Post process the output using one or more
                                post-processor(s), separated by commas.
  -q, --quiet                   Run with reduced output.
";
        public static void Main(string[] argv)
        {
            var fs = new FileSystem();
            var logger = new ConsoleLogger();
            var options = ParseOptions(argv);
            if (options.ContainsKey("--version"))
            {
                WriteVersion();
                return;
            }
            if (!options.ContainsKey("--python-file") &&
                !options.ContainsKey("--recursive"))
            {
                var xlator = new Translator("", "module_name", new(), fs, logger);
                xlator.Translate("-", Console.In, Console.Out);
                Console.Out.Flush();
                return;
            }
            var postProcessors = LoadPostProcessors(options, logger);
            var typeAnalysis = new AnalyzerImpl(fs, logger, options, DateTime.Now);
            if (options.TryGetValue("--recursive", out var oStartDir))
            {
                var startDir = (string) oStartDir;
                if (startDir == "." || startDir == "./" || startDir == ".\\")
                    startDir = Directory.GetCurrentDirectory();
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
                             postProcessors,
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
                if (!options.TryGetValue("<files>", out var oFiles) ||
                    oFiles is not List<string> files)
                    return;

                foreach (var file in files)
                {
                    typeAnalysis.LoadFileRecursive(file);
                }
                typeAnalysis.Finish();
                var types = new TypeReferenceTranslator(
                    typeAnalysis.BuildTypeDictionary());
                foreach (var file in files)
                {
                    var path = fs.GetFullPath(file);
                    var xlator = new Translator(
                        "",
                         fs.GetFileNameWithoutExtension(file),
                         postProcessors,
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

        private static IDictionary<string,object> ParseOptions(string[] args)
        {
            var result = new Dictionary<string, object>();
            var files = new List<string>();
            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (!arg.StartsWith('-'))
                {
                    files = args.Skip(i).ToList();
                    break;
                }
                switch (arg)
                {
                case "-v":
                case "--version":
                    result["--version"] = true;
                    break;
                case "-q":
                case "--quiet":
                    result["--quiet"] = true;
                    break;
                case "-r":
                case "--recursive":
                    var dirname = ".";
                    if (i < args.Length - 1)
                    {
                        if (!args[i + 1].StartsWith('-'))
                        {
                            ++i;
                            dirname = args[i];
                        }
                        break;
                    }
                    result["--recursive"] = dirname;
                    break;
                }
            }
            result["<files>"] = files;
            return result;
        }

        private static List<IPostProcessor> LoadPostProcessors(
            IDictionary<string, object> options,
            ILogger logger)
        {
            var result = new List<IPostProcessor>();
            if (!options.TryGetValue("--post-process", out var oPostProcessors))
                return result;

            foreach (string ppTypeName in ((string) oPostProcessors).Split(","))
            {
                var typeName = ppTypeName.Trim();
                try
                {
                    Type type = Type.GetType(typeName, true)!;
                    result.Add((IPostProcessor) Activator.CreateInstance(type)!);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Couldn't load postprocessor {typeName}.");
                }
            }
            return result;
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
