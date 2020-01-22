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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Translate;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Pytocs.Core
{
    /// <summary>
    ///     Top-level class responsible for translation from Python to C#.
    /// </summary>
    public class Translator
    {
        private readonly IFileSystem fs;
        private readonly ILogger logger;
        private readonly string moduleName;
        private readonly string nmspace;

        public Translator(string nmspace, string moduleName, IFileSystem fs, ILogger logger)
        {
            this.nmspace = nmspace;
            this.moduleName = moduleName;
            this.fs = fs;
            this.logger = logger;
        }

        public void Translate(string filename, TextReader input, TextWriter output)
        {
            Debug.Print("Translating module {0} in namespace {1}", moduleName, nmspace);
            Lexer lex = new Lexer(filename, input);
            CommentFilter flt = new CommentFilter(lex);
            Parser par = new Parser(filename, flt, true, logger);
            IEnumerable<Statement> stm = par.Parse();
            TypeReferenceTranslator types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            TranslateModuleStatements(stm, types, output);
        }

        public void TranslateModuleStatements(
            IEnumerable<Statement> stm,
            TypeReferenceTranslator types,
            string outputFileName)
        {
            TextWriter writer;
            try
            {
                writer = fs.CreateStreamWriter(fs.CreateFileStream(outputFileName, FileMode.Create, FileAccess.Write),
                    new UTF8Encoding(false));
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
            CodeCompileUnit unt = new CodeCompileUnit();
            CodeGenerator gen = new CodeGenerator(unt, nmspace, Path.GetFileNameWithoutExtension(moduleName));
            ModuleTranslator xlt = new ModuleTranslator(types, gen);
            xlt.Translate(stm);
            CSharpCodeProvider pvd = new CSharpCodeProvider();
            pvd.GenerateCodeFromCompileUnit(unt, output, new CodeGeneratorOptions());
        }

        public void TranslateFile(string inputFileName, string outputFileName)
        {
            TextReader reader = null;
            TextWriter writer = null;
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
                    writer = fs.CreateStreamWriter(
                        fs.CreateFileStream(outputFileName, FileMode.Create, FileAccess.Write),
                        new UTF8Encoding(false));
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
                if (writer != null)
                {
                    writer.Flush();
                    writer.Dispose();
                }

                if (reader != null)
                {
                    reader.Dispose();
                }
            }
        }

        public string TranslateSnippet(string snippet)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StringReader reader = new StringReader(snippet))
            using (StringWriter writer = new StringWriter(stringBuilder))
            {
                Translate("Program", reader, writer);
                writer.Flush();
            }

            return stringBuilder.ToString();
        }
    }
}