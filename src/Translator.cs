#region License
//  Copyright 2015-2018 John Källén
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

using Pytocs.CodeModel;
using Pytocs.Syntax;
using Pytocs.Translate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Pytocs.TypeInference;
using Pytocs.Types;

namespace Pytocs
{
    /// <summary>
    /// Top-level class responsible for translation from Python to C#.
    /// </summary>
    public class Translator
    {
        private string nmspace;
        private string moduleName;
        private IFileSystem fs;
        private ILogger logger;

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
            var lex = new Lexer(filename, input);
            var flt = new CommentFilter(lex);
            var par = new Parser(filename, flt);
            var stm = par.Parse();
            var types = new Dictionary<Node, DataType>();
            TranslateModuleStatements(stm, types, output);
        }

        public void TranslateModuleStatements(
            IEnumerable<Statement> stm,
            Dictionary<Node, DataType> types,
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
        }

        public void TranslateModuleStatements(
            IEnumerable<Statement> stm,
            Dictionary<Node,DataType> types, 
            TextWriter output)
        {
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, nmspace, Path.GetFileNameWithoutExtension(moduleName));
            var xlt = new ModuleTranslator(types, gen);
            xlt.Translate(stm);
            var pvd = new CSharpCodeProvider();
            pvd.GenerateCodeFromCompileUnit(unt, output, new CodeGeneratorOptions { });
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
    }
}
