using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CSharpCodeProvider : ICodeElementVisitor<int>
    {
        private IndentingTextWriter writer;
        //private CodeGeneratorOptions options;
        private CSharpStatementWriter stmWriter;
        private CSharpTypeWriter typeWriter;

        internal void GenerateCodeFromExpression(CodeExpression csExp, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            csExp.Accept(new CSharpExpressionWriter(this.writer));
        }

        internal void GenerateCodeFromType(CodeTypeDeclaration type, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            this.typeWriter = new CSharpTypeWriter(type, this.writer);
            type.Accept(typeWriter);
        }

        internal void GenerateCodeFromStatement(CodeStatement csStmt, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            stmWriter = new CSharpStatementWriter(this.writer);
            csStmt.Accept(stmWriter);
        }

        public int VisitNamespace(CodeNamespace n)
        {
            return 0;
        }

        public int VisitStatement(CodeStatement s)
        {
            return 0;
        }

        internal void GenerateCodeFromMember(CodeMember member, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            var memberWriter = new CSharpTypeWriter(null, this.writer);
            member.Accept(memberWriter);
        }

        internal void GenerateCodeFromCompileUnit(CodeCompileUnit compileUnit, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            var unitWriter = new CSharpUnitWriter(this, this.writer);
            unitWriter.Write(compileUnit);
        }
    }
}
