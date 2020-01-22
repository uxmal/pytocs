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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.Translate
{
    public class LocalVariableGenerator : ICodeStatementVisitor<int>
    {
        private readonly HashSet<string> globals;
        private readonly List<CodeParameterDeclarationExpression> parameters;
        private readonly Dictionary<CodeStatement, List<CodeStatement>> parentOf;
        private List<CodeStatement> path;
        private readonly Dictionary<CodeVariableReferenceExpression, List<List<CodeStatement>>> paths;
        private readonly List<CodeStatement> statements;

        private LocalVariableGenerator(
            List<CodeParameterDeclarationExpression> parameters,
            List<CodeStatement> statements,
            HashSet<string> globals)
        {
            this.parameters = parameters;
            this.statements = statements;
            this.globals = globals;
            parentOf = new Dictionary<CodeStatement, List<CodeStatement>>();
            paths = new Dictionary<CodeVariableReferenceExpression, List<List<CodeStatement>>>(
                new IdCmp());
        }

        public int VisitAssignment(CodeAssignStatement ass)
        {
            CodeVariableReferenceExpression id = ass.Destination as CodeVariableReferenceExpression;
            if (id == null)
            {
                return 0;
            }

            if (globals.Contains(id.Name))
            {
                return 0;
            }

            EnsurePath(id);
            return 0;
        }

        public int VisitBreak(CodeBreakStatement b)
        {
            return 0;
        }

        public int VisitComment(CodeCommentStatement codeCommentStatement)
        {
            return 0;
        }

        public int VisitContinue(CodeContinueStatement c)
        {
            return 0;
        }

        public int VisitForeach(CodeForeachStatement f)
        {
            Analyze(path, f.Statements);
            return 0;
        }

        public int VisitIf(CodeConditionStatement cond)
        {
            Analyze(path, cond.TrueStatements);
            Analyze(path, cond.FalseStatements);
            return 0;
        }

        public int VisitPostTestLoop(CodePostTestLoopStatement l)
        {
            Analyze(path, l.Body);
            return 0;
        }

        public int VisitPreTestLoop(CodePreTestLoopStatement l)
        {
            Analyze(path, l.Body);
            return 0;
        }

        public int VisitReturn(CodeMethodReturnStatement ret)
        {
            return 0;
        }

        public int VisitSideEffect(CodeExpressionStatement side)
        {
            return 0;
        }

        public int VisitThrow(CodeThrowExceptionStatement t)
        {
            return 0;
        }

        public int VisitTry(CodeTryCatchFinallyStatement t)
        {
            Analyze(path, t.TryStatements);
            foreach (CodeCatchClause c in t.CatchClauses)
            {
                Analyze(path, c.Statements);
            }

            Analyze(path, t.FinallyStatements);
            return 0;
        }

        public int VisitUsing(CodeUsingStatement codeUsingStatement)
        {
            return 0;
        }

        public int VisitVariableDeclaration(CodeVariableDeclarationStatement decl)
        {
            return 0;
        }

        public int VisitYield(CodeYieldStatement y)
        {
            return 0;
        }

        public static void Generate(CodeMemberMethod method, HashSet<string> globals)
        {
            LocalVariableGenerator gen = new LocalVariableGenerator(method.Parameters, method.Statements, globals);
            gen.Analyze(new List<CodeStatement>(), method.Statements);
            gen.Generate();
        }

        public static void Generate(List<CodeParameterDeclarationExpression> parameters, List<CodeStatement> statements,
            HashSet<string> globals)
        {
            parameters = parameters ?? new List<CodeParameterDeclarationExpression>();
            LocalVariableGenerator gen = new LocalVariableGenerator(parameters, statements, globals);
            gen.Analyze(new List<CodeStatement>(), statements);
            gen.Generate();
        }

        private void Analyze(List<CodeStatement> basePath, List<CodeStatement> statements)
        {
            foreach (CodeStatement stm in statements)
            {
                List<CodeStatement> opath = path;
                path = basePath.Concat(new[] { stm }).ToList();
                stm.Accept(this);
                parentOf[stm] = statements;
                path = opath;
            }
        }

        private void Generate()
        {
            HashSet<string> paramNames = new HashSet<string>(
                parameters
                    .Select(p => p.ParameterName));
            foreach (KeyValuePair<CodeVariableReferenceExpression, List<List<CodeStatement>>> de in paths.Where(
                d => !paramNames.Contains(d.Key.Name)))
            {
                CodeVariableReferenceExpression id = de.Key;
                List<List<CodeStatement>> defs = de.Value;
                if (defs.Count == 1)
                {
                    ReplaceWithDeclaration(id, defs[0], defs[0].Count - 1);
                }
                else
                {
                    List<CodeStatement> first = defs[0];
                    int iCommon = first.Count - 1;
                    foreach (List<CodeStatement> path in defs.Skip(1))
                    {
                        iCommon = FindCommon(first, path, iCommon);
                    }

                    ReplaceWithDeclaration(id, first, iCommon);
                }
            }
        }

        private int FindCommon(List<CodeStatement> a, List<CodeStatement> b, int iCommon)
        {
            iCommon = Math.Min(iCommon, a.Count - 1);
            iCommon = Math.Min(iCommon, b.Count - 1);
            int i;
            for (i = 0; i <= iCommon; ++i)
            {
                if (parentOf[a[i]] != parentOf[b[i]])
                {
                    return i - 1;
                }

                if (a[i] != b[i])
                {
                    return i;
                }
            }

            return i;
        }

        private void ReplaceWithDeclaration(CodeVariableReferenceExpression id, List<CodeStatement> path, int iCommon)
        {
            if (iCommon >= 0)
            {
                CodeStatement codeStatement = path[iCommon];
                List<CodeStatement> stms = parentOf[codeStatement];
                int i = stms.IndexOf(codeStatement);
                if (i >= 0)
                {
                    if (codeStatement is CodeAssignStatement ass)
                    {
                        // An assignment of type 'name = <exp>'. If the value
                        // assigned is null, use object type 'object', otherwise
                        // use 'var'.
                        CodePrimitiveExpression prim = ass.Source as CodePrimitiveExpression;
                        string dstType = prim != null && prim.Value == null
                            ? "object"
                            : "var";
                        stms[i] = new CodeVariableDeclarationStatement(dstType, id.Name)
                        {
                            InitExpression = ass.Source
                        };
                        return;
                    }
                }
            }

            statements.Insert(
                0,
                new CodeVariableDeclarationStatement("object", id.Name));
        }

        private void EnsurePath(CodeVariableReferenceExpression id)
        {
            if (!this.paths.TryGetValue(id, out List<List<CodeStatement>> paths))
            {
                paths = new List<List<CodeStatement>>();
                this.paths.Add(id, paths);
            }

            paths.Add(path);
        }

        internal class IdCmp : IEqualityComparer<CodeVariableReferenceExpression>
        {
            public bool Equals(CodeVariableReferenceExpression x, CodeVariableReferenceExpression y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(CodeVariableReferenceExpression obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}