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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Syntax
{
    class PyStatementWriter : IStatementVisitor
    {
        private readonly TextWriter writer;
        private Pytocs.Core.CodeModel.IndentingTextWriter w;

        public PyStatementWriter(TextWriter textWriter)
        {
            this.writer = textWriter;
            this.w = new CodeModel.IndentingTextWriter(textWriter);
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
            VisitDecorators(c.Decorators);
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
            w.Write("#{0}", c.Comment ?? "");
        }

        public void VisitContinue(ContinueStatement c)
        {
            throw new NotImplementedException();
        }

        public void VisitDecorators(List<Decorator>? decorators)
        {
            if (decorators is null)
                return;
            foreach (var dec in decorators)
            {
                w.Write("@");
                w.Write(dec.className.ToString());
                w.Write("(");
                var sep = "";
                foreach (var arg in dec.arguments)
                {
                    w.Write(sep);
                    sep = ", ";
                    arg.Write(writer);
                }
                w.Write(")");
                w.WriteLine();
            }
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
            f.Exprs.Write(writer);
            w.Write(" ");
            w.Write("in");
            w.Write(" ");
            f.Tests.Write(writer);
            w.WriteLine(":");
            ++w.IndentLevel;
            f.Body.Accept(this);
            --w.IndentLevel;
        }

        public void VisitFrom(FromStatement f)
        {
            w.Write("from");
            w.Write(" ");
            if (f.DottedName != null)
            {
            w.Write(f.DottedName.ToString());
            w.Write(" ");
            }
            w.Write("import");
            if (f.AliasedNames != null && f.AliasedNames.Count > 0)
            {
                w.Write(" (");
                var listSep = "";
                foreach (var aliasedName in f.AliasedNames)
                {
                    w.Write(listSep);
                    listSep = ", ";
                    var segSep = "";
                    foreach (var seg in aliasedName.Orig.segs)
                    {
                        w.Write(segSep);
                        segSep = ".";
                        w.Write(seg.Name);
                    }
                    if (aliasedName.Alias != null)
                    {
                        w.Write(" ");
                        w.Write("as");
                        w.Write(" ");
                        w.Write(aliasedName.Alias.Name);
                    }
                }
                w.Write(")");
            }
        }

        public void VisitFuncdef(FunctionDef f)
        {
            VisitDecorators(f.Decorators);
            w.Write("def");
            w.Write(" ");
            w.WriteName(f.name.Name);
            w.Write("(");
            var sep = "";
            foreach (var p in f.parameters)
            {
                w.Write(sep);
                sep = ",";
                w.Write(p.ToString());
                if (p.Annotation != null)
                {
                    w.Write(": ");
                    p.Annotation.Write(writer);
                }
            }
            w.Write(")");
            if (f.Annotation != null)
            {
                w.Write(" -> ");
                f.Annotation.Write(writer);
            }
            w.WriteLine(":");
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
                var elif = GetElif(i.Else);
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

        private IfStatement? GetElif(SuiteStatement s)
        {
            if (s.Statements.Count != 1)
                return null;
            return s.Statements[0] as IfStatement;
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
            var sep = " ";
            if (p.OutputStream != null)
            {
                w.Write(" >> ");
                p.OutputStream.Write(writer);
                sep = ", ";
            }
            foreach (var a in p.Args)
            {
                w.Write(sep);
                sep = ", ";
                a.Write(writer);
            }
            if (p.TrailingComma)
                w.Write(",");
        }

        public void VisitRaise(RaiseStatement r)
        {
            w.Write("raise");
            if (r.ExToRaise != null)
            {
                w.Write(" ");
                r.ExToRaise.Write(writer);
                if (r.ExOriginal != null)
                {
                    w.Write(", ");
                    r.ExOriginal.Write(writer);
                }
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
            foreach (var stm in s.Statements)
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
            t.Body.Accept(this);
            --w.IndentLevel;
            foreach (var h in t.ExHandlers)
            {
                w.Write("except");
                w.Write(" ");
                h.type!.Write(writer);
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

        public void VisitWhile(WhileStatement wh)
        {
            w.Write("while");
            w.Write(" ");
            wh.Test.Write(writer);
            w.WriteLine(":");
            ++w.IndentLevel;
            wh.Body.Accept(this);
        }

        public void VisitWith(WithStatement w)
        {
            this.w.Write("with");
            this.w.Write(" ");
            var sep = "";
            foreach (var ws in w.Items)
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
            w.Body.Accept(this);
            --this.w.IndentLevel;
        }

        public void VisitYield(YieldStatement y)
        {
            this.w.Write("yield");
            this.w.Write(" ");
            y.Expression.Write(writer);
        }

        public void VisitExec(ExecStatement exec)
        {
            w.Write("exec");
            w.Write(" ");
            exec.Code.Write(writer);
            if (exec.Globals != null)
            {
                w.Write(" ");
                w.Write("in");
                w.Write(" ");
                exec.Globals.Write(writer);
                if (exec.Locals != null)
                {
                    w.Write(", ");
                    exec.Locals.Write(writer);
                }
            }
        }
    }
}
