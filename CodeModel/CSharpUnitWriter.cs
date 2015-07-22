using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public class CSharpUnitWriter
    {
        private CSharpCodeProvider pvd;
        private IndentingTextWriter writer;

        public CSharpUnitWriter(CSharpCodeProvider pvd, IndentingTextWriter indentingTextWriter)
        {
            this.pvd = pvd;
            this.writer = indentingTextWriter;
        }
         
        public void Write(CodeCompileUnit unit)
        {
            foreach (var n in unit.Namespaces)
            {
                foreach (var comment in n.Comments)
                {
                    writer.Write("//");
                    writer.Write(comment.Comment);
                    writer.WriteLine();
                }
                if (!string.IsNullOrEmpty(n.Name))
                {
                    writer.Write("namespace");
                    writer.WriteName(" ");
                    writer.WriteDottedName(n.Name);
                    writer.WriteLine(" {");
                    ++writer.IndentLevel;
                }
                foreach (var imp in n.Imports)
                {
                    writer.WriteLine();
                    writer.Write("using");
                    writer.Write(" ");
                    writer.WriteDottedName(imp.Namespace);
                    writer.WriteLine(";");
                }
                foreach (var type in n.Types)
                {
                    writer.WriteLine();
                    var tw = new CSharpTypeWriter(type, writer);
                    type.Accept(tw);
                }
                if (!string.IsNullOrEmpty(n.Name))
                {
                    --writer.IndentLevel;
                    writer.WriteLine("}");
                }
            }
        }
    }
}
