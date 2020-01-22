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

using Pytocs.Core.CodeModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Pytocs.Core.Syntax
{
    internal class PyStatementWriter : IStatementVisitor
    {
        private readonly IndentingTextWriter w;
        private readonly TextWriter writer;

        public PyStatementWriter(TextWriter textWriter)
        {
            writer = textWriter;
            w = new IndentingTextWriter(textWriter);
        }

        public void VisitAssert(AssertStatement a)
        {
            throw new NotImplementedException();
        }

        public void VisitAsync(AsyncStatement a)
        {
            w.Write("async");
            w.Write(" ");
            a.Statement.Accept(this);
        }

        public void VisitBreak(BreakStatement b)
        {
            throw new NotImplementedException();
        }

        public void VisitClass(ClassDef c)
        {
            VisitDecorators(c.decorators);
            w.Write("class");
            w.Write(" ");
            w.Write(c.name.Name);
            if (c.args != null && c.args.Count > 0)
            {
                w.Write("(");
                w.Write(string.Join(",", c.args.Select(e => e.ToString())));
                w.Write(")");
            }

            w.WriteLine(":");
            ++w.IndentLevel;
            c.body.Accept(this);
            --w.IndentLevel;
        }

        public void VisitComment(CommentStatement c)
        {
            w.Write("#{0}", c.comment);
        }

        public void VisitContinue(ContinueStatement c)
        {
            throw new NotImplementedException();
        }

        public void VisitDel(DelStatement d)
        {
            throw new NotImplementedException();
        }

        public void VisitExp(ExpStatement e)
        {
            // forces any pending indentation to be emitted
            // since expressions are unaware of indenting writers.
            w.Write("");
            e.Expression.Write(writer);
        }

        public void VisitFor(ForStatement f)
        {
            w.Write("for");
            w.Write(" ");
            f.exprs.Write(writer);
            w.Write(" ");
            w.Write("in");
            w.Write(" ");
            f.tests.Write(writer);
            w.WriteLine(":");
            ++w.IndentLevel;
            f.Body.Accept(this);
            --w.IndentLevel;
        }

        public void VisitFrom(FromStatement f)
        {
            w.Write("from");
            w.Write(" ");
            w.Write(f.DottedName.ToString());
            w.Write(" ");
            w.Write("import");
            if (f.AliasedNames != null && f.AliasedNames.Count > 0)
            {
                w.Write(" (");
                string listSep = "";
                foreach (AliasedName aliasedName in f.AliasedNames)
                {
                    w.Write(listSep);
                    listSep = ", ";
                    string segSep = "";
                    foreach (Identifier seg in aliasedName.orig.segs)
                    {
                        w.Write(segSep);
                        segSep = ".";
                        w.Write(seg.Name);
                    }

                    if (aliasedName.alias != null)
                    {
                        w.Write(" ");
                        w.Write("as");
                        w.Write(" ");
                        w.Write(aliasedName.alias.Name);
                    }
                }

                w.Write(")");
            }
        }

        public void VisitFuncdef(FunctionDef f)
        {
            VisitDecorators(f.decorators);
            w.Write("def");
            w.Write(" ");
            w.WriteName(f.name.Name);
            w.Write("(");
            string sep = "";
            foreach (Parameter p in f.parameters)
            {
                w.Write(sep);
                sep = ",";
                w.Write(p.ToString());
            }

            w.WriteLine("):");
            ++w.IndentLevel;
            f.body.Accept(this);
            --w.IndentLevel;
        }

        public void VisitGlobal(GlobalStatement g)
        {
            throw new NotImplementedException();
        }

        public void VisitIf(IfStatement i)
        {
            w.Write("if");
            w.Write(" ");
            i.Test.Write(writer);
            w.WriteLine(":");
            ++w.IndentLevel;
            i.Then.Accept(this);
            --w.IndentLevel;
            while (i.Else != null)
            {
                IfStatement elif = GetElif(i.Else);
                if (elif != null)
                {
                    w.Write("elif");
                    w.Write(" ");
                    elif.Test.Write(writer);
                    w.WriteLine(":");
                    ++w.IndentLevel;
                    elif.Then.Accept(this);
                    --w.IndentLevel;
                    i = elif;
                }
                else
                {
                    w.Write("else");
                    w.WriteLine(":");
                    ++w.IndentLevel;
                    i.Else.Accept(this);
                    --w.IndentLevel;
                    break;
                }
            }
        }

        public void VisitImport(ImportStatement i)
        {
            throw new NotImplementedException();
        }

        public void VisitNonLocal(NonlocalStatement n)
        {
            throw new NotImplementedException();
        }

        public void VisitPass(PassStatement p)
        {
            w.Write("pass");
        }

        public void VisitPrint(PrintStatement p)
        {
            w.Write("print");
            string sep = " ";
            if (p.outputStream != null)
            {
                w.Write(" >> ");
                p.outputStream.Write(writer);
                sep = ", ";
            }

            foreach (Argument a in p.args)
            {
                w.Write(sep);
                sep = ", ";
                a.Write(writer);
            }

            if (p.trailingComma)
            {
                w.Write(",");
            }
        }

        public void VisitRaise(RaiseStatement r)
        {
            w.Write("raise");
            w.Write(" ");
            r.exToRaise.Write(writer);
            if (r.exOriginal != null)
            {
                w.Write(", ");
                r.exOriginal.Write(writer);
            }
        }

        public void VisitReturn(ReturnStatement r)
        {
            w.Write("return");
            if (r.Expression != null)
            {
                w.Write(" ");
                r.Expression.Write(writer);
            }
        }

        public void VisitSuite(SuiteStatement s)
        {
            foreach (Statement stm in s.stmts)
            {
                stm.Accept(this);
                if (!(stm is SuiteStatement))
                {
                    w.WriteLine();
                }
            }
        }

        public void VisitTry(TryStatement t)
        {
            w.Write("try");
            w.WriteLine(":");
            ++w.IndentLevel;
            t.body.Accept(this);
            --w.IndentLevel;
            foreach (ExceptHandler h in t.exHandlers)
            {
                w.Write("except");
                w.Write(" ");
                h.type.Write(writer);
                if (h.name != null)
                {
                    w.Write(" ");
                    w.Write("as");
                    w.Write(" ");
                    w.Write(h.name.Name);
                }

                w.WriteLine(":");
                ++w.IndentLevel;
                h.body.Accept(this);
                --w.IndentLevel;
            }
        }

        public void VisitWhile(WhileStatement w)
        {
            throw new NotImplementedException();
        }

        public void VisitWith(WithStatement w)
        {
            this.w.Write("with");
            this.w.Write(" ");
            string sep = "";
            foreach (WithItem ws in w.items)
            {
                this.w.Write(sep);
                sep = ", ";
                ws.t.Write(writer);
                if (ws.e != null)
                {
                    this.w.Write(" ");
                    this.w.Write("as");
                    this.w.Write(" ");
                    ws.e.Write(writer);
                }
            }

            this.w.Write(":");
            this.w.WriteLine();
            ++this.w.IndentLevel;
            w.body.Accept(this);
            --this.w.IndentLevel;
        }

        public void VisitYield(YieldStatement y)
        {
            w.Write("yield");
            w.Write(" ");
            y.Expression.Write(writer);
        }

        public void VisitExec(ExecStatement exec)
        {
            w.Write("exec");
            w.Write(" ");
            exec.code.Write(writer);
            if (exec.globals != null)
            {
                w.Write(" ");
                w.Write("in");
                w.Write(" ");
                exec.globals.Write(writer);
                if (exec.locals != null)
                {
                    w.Write(", ");
                    exec.locals.Write(writer);
                }
            }
        }

        public void VisitDecorators(List<Decorator> decorators)
        {
            if (decorators == null)
            {
                return;
            }

            foreach (Decorator dec in decorators)
            {
                w.Write("@");
                w.Write(dec.className.ToString());
                w.Write("(");
                string sep = "";
                foreach (Argument arg in dec.arguments)
                {
                    w.Write(sep);
                    sep = ", ";
                    arg.Write(writer);
                }

                w.Write(")");
                w.WriteLine();
            }
        }

        private IfStatement GetElif(SuiteStatement s)
        {
            if (s.stmts.Count != 1)
            {
                return null;
            }

            return s.stmts[0] as IfStatement;
        }
    }
}