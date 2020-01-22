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

using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using Pytocs.Core.TypeInference;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Pytocs.Core;

namespace Pytocs.Gui
{
    //$REFACTOR: Extract common code from Cli and Gui application
    public static class ConversionUtils
    {
        public static async Task ConvertFolderAsync(string sourcePath, string targetPath, ILogger logger)
        {
            try
            {
                FileSystem fs = new FileSystem();
                Dictionary<string, object> options = new Dictionary<string, object>();
                AnalyzerImpl typeAnalysis = new AnalyzerImpl(fs, logger, options, DateTime.Now);
                typeAnalysis.Analyze(sourcePath);
                typeAnalysis.Finish();
                TypeReferenceTranslator types = new TypeReferenceTranslator(typeAnalysis.BuildTypeDictionary());

                var walker = new DirectoryWalker(fs, sourcePath, "*.py");
                await walker.EnumerateAsync(state =>
                {
                    foreach (string file in fs.GetFiles(state.DirectoryName, "*.py", SearchOption.TopDirectoryOnly))
                    {
                        string path = fs.GetFullPath(file);
                        var xlator = new Translator(
                            state.Namespace,
                            fs.GetFileNameWithoutExtension(file),
                            fs,
                            logger);
                        Module module = typeAnalysis.GetAstForFile(path);

                        string relativePath = MakeRelative(sourcePath, path);
                        string targetFilePath = Path.ChangeExtension(MakeAbsolute(targetPath, relativePath), ".py.cs");
                        string targetFileDirectory = Path.GetDirectoryName(targetFilePath);

                        if (!Directory.Exists(targetFileDirectory))
                        {
                            Directory.CreateDirectory(targetFileDirectory);
                        }

                        xlator.TranslateModuleStatements(
                            module.body.stmts,
                            types,
                            targetFilePath);
                    }
                });

                logger.Inform(Resources.Done);
            }
            catch (Exception ex)
            {
                logger.Error(Resources.ErrUnexpectedConversionError, ex.Message);
                logger.Inform(Resources.ConversionWasAborted);
            }
        }

        private static string MakeRelative(string basePath, string filePath)
        {
            return Path.GetFullPath(filePath)
                .Replace(Path.GetFullPath(basePath), string.Empty)
                .TrimStart('\\')
                .TrimStart('/');
        }

        private static string MakeAbsolute(string basePath, string filePath)
        {
            return Path.Combine(basePath, filePath);
        }
    }
}