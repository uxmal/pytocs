#region License
//  Copyright 2015-2022 John Källén
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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pytocs.Core.Translate
{
    public class ModuleTranslator : StatementTranslator
    {
        private readonly CodeGenerator gen;

        public ModuleTranslator(TypeReferenceTranslator types, CodeGenerator gen) : 
            base(null, types, gen, new SymbolGenerator(), new HashSet<string>())
        {
            this.gen = gen;
        }

        public void Translate(IEnumerable<Statement> statements)
        {
            var stms = statements.ToList();
            var fields = AllSimpleAssignments(stms);
            if (fields is null)
            {
                int c = 0;
                foreach (var s in stms)
                {
                    if (c == 0 && IsStringStatement(s, out Str lit))
                    {
                    GenerateDocComment(lit.Value, gen.CurrentNamespace.Comments);
                    }
                    else
                    {
                        s.Accept(this);
                    }
                    ++c;
                }
                if (gen.Scope.Count > 0)
                {
                    // Module-level statements are simulated with a static constructor.
                    var methodName = gen.CurrentType.Name!;
                    var parameters = Array.Empty<CodeParameterDeclarationExpression>();
                    var static_ctor = gen.StaticMethod(methodName, parameters, null, () => { });
                    static_ctor.Attributes = MemberAttributes.Static;
                    static_ctor.Statements.AddRange(gen.Scope);
                }
            }
            else
            {
                // We have statements other than simple assignments. Generate
                // definitions for all fields, then put all the code in the constructor.
                int c = 0;
                foreach (var s in stms)
                {
                    if (c == 0 && IsStringStatement(s, out Str lit))
                    {
                        GenerateDocComment(lit.Value, gen.CurrentNamespace.Comments);
                    }
                    else if (IsAssignment(s, out AssignExp? ass) &&
                        ass.Dst is Identifier id)
                    {
                        var (fieldType, nmspcs) = base.types.TranslateTypeOf(id);
                        gen.EnsureImports(nmspcs);

                        GenerateField(id.Name, fieldType, null);
                    }
                    ++c;
                }

                var methodName = gen.CurrentType.Name!;
                var parameters = Array.Empty<CodeParameterDeclarationExpression>();
                this.GenerateFieldForAssignment = false;
                var static_ctor = gen.StaticMethod(methodName, parameters, null, () =>
                {
                    foreach (var stm in stms)
                    {
                        stm.Accept(this);
                    }
                });
                static_ctor.Attributes = MemberAttributes.Static;
            }
        }

        // From a list of Python statements, extract all statements that are simple assignments.
        // a = b
        private List<(Identifier, Exp, string?)>? AllSimpleAssignments(List<Statement> stms)
        {
            bool sawOnlySimpleAssignments = true;
            var result = new List<(Identifier, Exp, string?)>();
            foreach (var stm in stms)
            {
                if (stm is SuiteStatement sstm)
                {
                    switch (sstm.Statements[0])
                    {
                    case ExpStatement estm:
                        switch (estm.Expression)
                        {
                        case AssignExp ass when ass.Dst is Identifier id:
                            result.Add((id, ass.Src!, ass.Comment));
                            break;
                        case AssignExp:
                            sawOnlySimpleAssignments = false;
                            break;
                        case Str:
                            break;
                        }
                        break;
                    case PrintStatement:
                        sawOnlySimpleAssignments = false;
                        break;
                    }
                }
            }
            return sawOnlySimpleAssignments ? null : result;
        }

        public void GenerateDocComment(string text, List<CodeCommentStatement> comments)
        {
            var lines = text.Replace("\r\n", "\n")
                .Split('\r', '\n')
                .Select(line => new CodeCommentStatement(" " + line));
            comments.AddRange(lines);
        }

        public bool IsStringStatement(Statement s, out Str lit)
        {
            lit = null!;
            var strStmt = s as ExpStatement;
            if (strStmt is null)
            {
                if (s is not SuiteStatement suite)
                    return false;
                strStmt = suite.Statements[0] as ExpStatement;
                if (strStmt == null)
                    return false;
            }
            lit = (strStmt.Expression as Str)!;
            return lit != null;
        }

        public bool IsAssignment(Statement s, [MaybeNullWhen(false)] out AssignExp ass)
        {
            if (s is SuiteStatement ss)
            {
                s = ss.Statements[0];
            }
            if (s is ExpStatement es && es.Expression is AssignExp a)
            {
                ass = a;
                return true;
            }
            else
            {
                ass = null;
                return false;
            }
        }

        protected override CodeMemberField GenerateField(string name, CodeTypeReference fieldType, CodeExpression? value)
        {
            var field = base.GenerateField(name, fieldType, value);
            field.Attributes |= MemberAttributes.Static;
            return field;
        }
    }
}
