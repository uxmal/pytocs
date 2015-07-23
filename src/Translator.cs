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

namespace Pytocs
{
    public class Translator
    {
        private string nmspace;
        private string moduleName;
        private ILogger logger;

        public Translator(string nmspace, string moduleName, ILogger logger)
        {
            this.nmspace = nmspace;
            this.moduleName = moduleName;
            this.logger = logger;
        }

        public void Translate(string filename, TextReader input, TextWriter output)
        {
            Debug.Print("Translating module {0} in namespace {1}", moduleName, nmspace);
            var lex = new Syntax.Lexer(filename, input);
            var par = new Syntax.Parser(filename, lex);
            var stm = par.Parse();
            var unt = new CodeCompileUnit();
            var gen = new CodeGenerator(unt, nmspace, Path.GetFileNameWithoutExtension(moduleName));
            var xlt = new ModuleTranslator(gen);
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
                    reader = new StreamReader(inputFileName);
                }
                catch (IOException ex)
                {
                    logger.Error(ex, "Unable to open file {0} for reading.", inputFileName);
                    return;
                }
                try 
                {
                    writer = new StreamWriter(new FileStream(outputFileName, FileMode.Create, FileAccess.Write), new UTF8Encoding(false));
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
                if (writer != null) writer.Dispose();
                if (reader != null) reader.Dispose();
            }
        }
    }
}
