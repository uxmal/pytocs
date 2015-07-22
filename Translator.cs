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

namespace Pytocs
{
    public class Translator
    {
        private string nmspace;
        private string moduleName;

        public Translator(string nmspace, string moduleName)
        {
            this.nmspace = nmspace;
            this.moduleName = moduleName;
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
                reader = new StreamReader(inputFileName);
                writer = new StreamWriter(new FileStream(outputFileName, FileMode.Create, FileAccess.Write), new UTF8Encoding(false));
                Translate(inputFileName, reader, writer);
            }
            catch (Exception)
            {
                // Diagnostic.
            }
            finally
            {
                if (writer != null) writer.Dispose();
                if (reader != null) reader.Dispose();
            }
        }
    }
}
