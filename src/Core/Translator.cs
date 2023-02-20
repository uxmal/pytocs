#region License
//  Copyright 2015-2021 John Källén
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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;

namespace Pytocs.Core
{
    /// <summary>
    /// Top-level class responsible for translation from Python to C#.
    /// </summary>
    public class Translator
    {
        private readonly string nmspace;
        private readonly string moduleName;
        private List<IPostProcessor> postProcessors;
        private readonly IFileSystem fs;
        private readonly ILogger logger;

        public Translator(
            string nmspace,
            string moduleName,
            List<IPostProcessor> postProcessors,
            IFileSystem fs,
            ILogger logger)
        {
            this.nmspace = nmspace;
            this.moduleName = moduleName;
            this.postProcessors = postProcessors;
            this.fs = fs;
            this.logger = logger;
        }

        public void Translate(string filename, TextReader input, TextWriter output)
        {
            Debug.Print("Translating module {0} in namespace {1}", moduleName, nmspace);
            try
            {
                var lex = new Lexer(filename, input);
                var flt = new CommentFilter(lex);
                var par = new Parser(filename, flt, true, logger);
                var stm = par.Parse();
                var types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
                TranslateModuleStatements(stm, types, output);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "An error occurred when translating module {0} (in {1}).", moduleName, filename);
            }
        }

        public void TranslateModuleStatements(
            IEnumerable<Statement> stm,
            TypeReferenceTranslator types,
            string outputFileName)
        {
            TextWriter writer;
            try
            {
                writer = fs.CreateStreamWriter(fs.CreateFileStream(outputFileName, FileMode.Create, FileAccess.Write), new UTF8Encoding(false));
            }
            catch (IOException ex)
            {
                logger.Error(ex, "Unable to open file {0} for writing.", outputFileName);
                return;
            }
            try
            {
                TranslateModuleStatements(stm, types, writer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                writer.Close();
        }
        }

        public void TranslateModuleStatements(
            IEnumerable<Statement> stm,
            TypeReferenceTranslator types,
            TextWriter output)
        {
            try
            {
                var unt = new CodeCompileUnit();
                var gen = new CodeGenerator(unt, nmspace, Path.GetFileNameWithoutExtension(moduleName));
                var xlt = new ModuleTranslator(types, gen);
                xlt.Translate(stm);
                unt = PostProcess(unt);
                var pvd = new CSharpCodeProvider();
                pvd.GenerateCodeFromCompileUnit(unt, output, new CodeGeneratorOptions { });
            }
            catch (NodeException nex)
            {
                logger.Error($"{nex.Node.Filename}({nex.Node.Start}): {nex.Message}");
            }
        }

        private CodeCompileUnit PostProcess(CodeCompileUnit unt)
        {
            foreach (var postProcessor in this.postProcessors)
            {
                try
                {
                    unt = postProcessor.PostProcess(unt);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"An error occurred during post-processing with {postProcessor.GetType().Name}. Post-processing will stop.");
                }
            }
            return unt;
        }

        public void TranslateFile(string inputFileName, string outputFileName)
        {
            TextReader? reader = null;
            TextWriter? writer = null;
            try
            {
                try
                {
                    reader = fs.CreateStreamReader(inputFileName);
                }
                catch (IOException ex)
                {
                    logger.Error(ex, "Unable to open file {0} for reading.", inputFileName);
                    return;
                }
                try 
                {
                    writer = fs.CreateStreamWriter(fs.CreateFileStream(outputFileName, FileMode.Create, FileAccess.Write), new UTF8Encoding(false));
                }
                catch (IOException ex)
                {
                    logger.Error(ex, "Unable to open file {0} for writing.", outputFileName);
                    return;
                }
                Translate(inputFileName, reader, writer);
            }
            
            finally
            {
                if (writer != null) { writer.Flush(); writer.Dispose(); }
                if (reader != null) reader.Dispose();
            }
        }

        public string TranslateSnippet(string snippet)
        {
            var stringBuilder = new StringBuilder();
            using (var reader = new StringReader(snippet))
            using (var writer = new StringWriter(stringBuilder))
            {
                Translate("Program", reader, writer);
                writer.Flush();
            }

            return stringBuilder.ToString();
        }
    }
}
