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

using System.Collections.Generic;

namespace Pytocs.Core.CodeModel
{
    public class CSharpStatementWriter : ICodeStatementVisitor<int>
    {
        private readonly CSharpExpressionWriter expWriter;
        private bool suppressSemi;
        private readonly IndentingTextWriter writer;

        public CSharpStatementWriter(IndentingTextWriter writer)
        {
            this.writer = writer;
            expWriter = new CSharpExpressionWriter(writer);
        }

        public int VisitAssignment(CodeAssignStatement ass)
        {
            ass.Destination.Accept(expWriter);
            writer.Write(" = ");
            ass.Source.Accept(expWriter);
            EndLineWithSemi();
            return 0;
        }

        public int VisitBreak(CodeBreakStatement b)
        {
            writer.Write("break");
            EndLineWithSemi();
            return 0;
        }

        public int VisitComment(CodeCommentStatement c)
        {
            writer.Write("//");
            writer.Write(c.Comment);
            TerminateLine();
            return 0;
        }

        public int VisitContinue(CodeContinueStatement c)
        {
            writer.Write("continue");
            EndLineWithSemi();
            return 0;
        }

        public int VisitForeach(CodeForeachStatement f)
        {
            writer.Write("foreach");
            writer.Write(" (");
            writer.Write("var");
            writer.Write(" ");
            f.Variable.Accept(expWriter);
            writer.Write(" ");
            writer.Write("in");
            writer.Write(" ");
            f.Collection.Accept(expWriter);
            writer.Write(")");
            WriteStatements(f.Statements);
            writer.WriteLine();
            return 0;
        }

        public int VisitIf(CodeConditionStatement cond)
        {
            writer.Write("if");
            writer.Write(" (");
            cond.Condition.Accept(expWriter);
            writer.Write(")");
            WriteStatements(cond.TrueStatements);
            if (cond.FalseStatements.Count > 0)
            {
                writer.Write(" ");
                writer.Write("else");
                if (cond.FalseStatements.Count == 1)
                {
                    CodeConditionStatement elseIf = cond.FalseStatements[0] as CodeConditionStatement;
                    if (elseIf != null)
                    {
                        writer.Write(" ");
                        return VisitIf(elseIf);
                    }
                }

                WriteStatements(cond.FalseStatements);
            }

            writer.WriteLine();
            return 0;
        }

        public int VisitTry(CodeTryCatchFinallyStatement t)
        {
            writer.Write("try");
            WriteStatements(t.TryStatements);
            foreach (CodeCatchClause clause in t.CatchClauses)
            {
                WriteCatchClause(clause);
            }

            if (t.FinallyStatements.Count > 0)
            {
                writer.Write(" ");
                writer.Write("finally");
                WriteStatements(t.FinallyStatements);
            }

            writer.WriteLine();
            return 0;
        }

        public int VisitPostTestLoop(CodePostTestLoopStatement loop)
        {
            writer.Write("do");
            WriteStatements(loop.Body);
            writer.Write(" ");
            writer.Write("while");
            writer.Write(" (");
            loop.Test.Accept(expWriter);
            writer.Write(");");
            TerminateLine();
            return 0;
        }

        public int VisitPreTestLoop(CodePreTestLoopStatement loop)
        {
            writer.Write("while");
            writer.Write(" (");
            loop.Test.Accept(expWriter);
            writer.Write(")");
            WriteStatements(loop.Body);
            writer.WriteLine();
            return 0;
        }

        public int VisitReturn(CodeMethodReturnStatement ret)
        {
            writer.Write("return");
            if (ret.Expression != null)
            {
                writer.Write(" ");
                ret.Expression.Accept(expWriter);
            }

            EndLineWithSemi();
            return 0;
        }

        public int VisitSideEffect(CodeExpressionStatement side)
        {
            side.Expression.Accept(expWriter);
            EndLineWithSemi();
            return 0;
        }

        public int VisitThrow(CodeThrowExceptionStatement t)
        {
            writer.Write("throw");
            if (t.Expression != null)
            {
                writer.Write(" ");
                t.Expression.Accept(expWriter);
            }

            EndLineWithSemi();
            return 0;
        }

        public int VisitUsing(CodeUsingStatement u)
        {
            writer.Write("using");
            writer.Write(" (");
            writer.Write("var");
            writer.Write(" ");
            string sep = "";
            bool old = suppressSemi;
            suppressSemi = true;
            foreach (CodeStatement init in u.Initializers)
            {
                writer.Write(sep);
                sep = ", ";
                init.Accept(this);
            }

            suppressSemi = old;
            writer.Write(")");
            WriteStatements(u.Statements);
            writer.WriteLine();
            return 0;
        }

        public int VisitVariableDeclaration(CodeVariableDeclarationStatement decl)
        {
            expWriter.VisitTypeReference(decl.Type);
            writer.Write(" ");
            writer.WriteName(decl.Name);
            if (decl.InitExpression != null)
            {
                writer.Write(" = ");
                decl.InitExpression.Accept(expWriter);
            }

            writer.WriteLine(";");
            return 0;
        }

        public int VisitYield(CodeYieldStatement y)
        {
            writer.Write("yield");
            writer.Write(" ");
            writer.Write("return");
            writer.Write(" ");
            y.Expression.Accept(expWriter);
            EndLineWithSemi();
            return 0;
        }

        private void EndLineWithSemi()
        {
            if (!suppressSemi)
            {
                writer.Write(";");
                TerminateLine();
            }
        }

        private void TerminateLine()
        {
            writer.WriteLine();
        }

        private void WriteCatchClause(CodeCatchClause clause)
        {
            writer.Write(" ");
            writer.Write("catch");
            if (clause.CatchExceptionType != null)
            {
                writer.Write(" (");
                expWriter.VisitTypeReference(clause.CatchExceptionType);
                if (!string.IsNullOrEmpty(clause.LocalName))
                {
                    writer.Write(" ");
                    writer.WriteName(clause.LocalName);
                }

                writer.Write(")");
            }

            WriteStatements(clause.Statements);
        }

        public void WriteStatements(List<CodeStatement> stm)
        {
            writer.Write(" {");
            TerminateLine();
            ++writer.IndentLevel;
            foreach (CodeStatement s in stm)
            {
                s.Accept(this);
            }

            --writer.IndentLevel;
            writer.Write("}");
        }
    }
}