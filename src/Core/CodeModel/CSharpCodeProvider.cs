﻿#region License

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

using System.IO;

namespace Pytocs.Core.CodeModel
{
    public class CSharpCodeProvider : ICodeElementVisitor<int>
    {
        private CSharpStatementWriter stmWriter;
        private CSharpTypeWriter typeWriter;
        private IndentingTextWriter writer;

        public int VisitNamespace(CodeNamespace n)
        {
            return 0;
        }

        public int VisitStatement(CodeStatement s)
        {
            return 0;
        }

        public void GenerateCodeFromExpression(CodeExpression csExp, TextWriter writer,
            CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            csExp.Accept(new CSharpExpressionWriter(this.writer));
        }

        public void GenerateCodeFromType(CodeTypeDeclaration type, TextWriter writer,
            CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            typeWriter = new CSharpTypeWriter(type, this.writer);
            type.Accept(typeWriter);
        }

        public void GenerateCodeFromStatement(CodeStatement csStmt, TextWriter writer,
            CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            stmWriter = new CSharpStatementWriter(this.writer);
            csStmt.Accept(stmWriter);
        }

        public void GenerateCodeFromMember(CodeMember member, TextWriter writer,
            CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            CSharpTypeWriter memberWriter = new CSharpTypeWriter(null, this.writer);
            member.Accept(memberWriter);
        }

        public void GenerateCodeFromCompileUnit(CodeCompileUnit compileUnit, TextWriter writer,
            CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            CSharpUnitWriter unitWriter = new CSharpUnitWriter(this.writer);
            unitWriter.Write(compileUnit);
        }
    }
}