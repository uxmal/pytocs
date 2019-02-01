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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate
{
    public class ModuleTranslator : StatementTranslator
    {
        private CodeGenerator gen;

        public ModuleTranslator(TypeReferenceTranslator types, CodeGenerator gen) : base(types, gen, new SymbolGenerator(), new HashSet<string>())
        {
            this.gen = gen;
        }

        public void Translate(IEnumerable<Statement> statements)
        {
            int c = 0;
            foreach (var s in statements)
            {
                if (c == 0 && IsStringStatement(s, out Str lit))
                {
                    GenerateDocComment(lit.s, gen.CurrentNamespace.Comments);
                }
                else
                {
                    s.Accept(this);
                }
                ++c;
            }
        }

        public void GenerateDocComment(string text, List<CodeCommentStatement> comments)
        {
            var lines = text.Replace("\r\n", "\n").Split('\r', '\n').Select(line => new CodeCommentStatement(" " + line));
            comments.AddRange(lines);
        }

        public bool IsStringStatement(Statement s, out Str lit)
        {
            lit = null;
            var strStmt = s as ExpStatement;
            if (strStmt == null)
            {
                var suite = s as SuiteStatement;
                if (suite == null)
                    return false;
                strStmt = suite.stmts[0] as ExpStatement;
                if (strStmt == null)
                    return false;
            }
            lit = strStmt.Expression as Str;
            return lit != null;
        }

        protected override CodeMemberField GenerateField(string name, CodeTypeReference fieldType, CodeExpression value)
        {
            var field = base.GenerateField(name, fieldType, value);
            field.Attributes |= MemberAttributes.Static;
            return field;
        }
    }
}
