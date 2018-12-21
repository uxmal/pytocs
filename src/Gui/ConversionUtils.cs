using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Pytocs.Translate;
using Pytocs.TypeInference;

namespace Pytocs.Gui
{
    public static class ConversionUtils
    {
        public static async Task ConvertFolder(string sourcePath, string targetPath, ILogger logger)
        {
            var fs = new FileSystem();
            var options = new Dictionary<string, object>();
            var typeAnalysis = new AnalyzerImpl(fs, logger, options, DateTime.Now);
            typeAnalysis.Analyze(sourcePath);
            typeAnalysis.Finish();
            var types = new TypeReferenceTranslator(typeAnalysis.BuildTypeDictionary());

            var walker = new DirectoryWalker(fs, sourcePath, "*.py");
            await walker.EnumerateAsync(state =>
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

                    var relativePath = MakeRelative(sourcePath, path);
                    var targetFilePath = Path.ChangeExtension(MakeAbsolute(targetPath, relativePath), ".py.cs");
                    var targetFileDirectory = Path.GetDirectoryName(targetFilePath);

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

            //todo: use a configuration file for processing message
            logger.Inform("Done!");
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
