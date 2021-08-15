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

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.CodeModel
{
    public class CSharpCodeProvider : ICodeElementVisitor<int>
    {
        private IndentingTextWriter? writer;
        private CSharpStatementWriter? stmWriter;
        private CSharpTypeWriter? typeWriter;

        public void GenerateCodeFromExpression(CodeExpression csExp, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            csExp.Accept(new CSharpExpressionWriter(this.writer));
        }

        public void GenerateCodeFromType(CodeTypeDeclaration type, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            this.typeWriter = new CSharpTypeWriter(type, this.writer);
            type.Accept(typeWriter);
        }

        public void GenerateCodeFromStatement(CodeStatement csStmt, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
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

        public void GenerateCodeFromMember(CodeMember member, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            var memberWriter = new CSharpTypeWriter(null, this.writer);
            member.Accept(memberWriter);
        }

        public void GenerateCodeFromCompileUnit(CodeCompileUnit compileUnit, TextWriter writer, CodeGeneratorOptions codeGeneratorOptions)
        {
            this.writer = new IndentingTextWriter(writer);
            var unitWriter = new CSharpUnitWriter(this.writer);
            unitWriter.Write(compileUnit);
        }
    }
}
