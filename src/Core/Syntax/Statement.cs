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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Syntax
{
    public abstract class Statement : Node
    {
        public string comment;
        public List<Decorator> decorators;

        public abstract T Accept<T>(IStatementVisitor<T> v);
        public abstract void Accept(IStatementVisitor v);

        public Statement(string filename, int start, int end) : base(filename, start, end) { } 

        public sealed override string ToString() { var sw = new StringWriter(); Accept(new PyStatementWriter(sw)); return sw.ToString(); }
    }

    public class AsyncStatement : Statement
    {
        public Statement Statement;

        public AsyncStatement(Statement stmt, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Statement = stmt;
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitAsync(this);
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitAsync(this);
        }
    }

    public class AssertStatement : Statement
    {
        public AssertStatement(List<Exp> e, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.Tests = e;
        }

        public List<Exp> Tests { get; set; }
        public Exp Message { get; set; }    //$TODO: initialize this?

        public override void Accept(IStatementVisitor v)
        {
            v.VisitAssert(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitAssert(this);
        }
    }

    public class BreakStatement : Statement
    {
        public BreakStatement(string filename, int start, int end) : base(filename, start, end) { }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitBreak(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitBreak(this);
        }
    }

    public class CommentStatement : Statement
    {
        public CommentStatement(string filename, int start, int end) : base(filename, start, end) { }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitComment(this);
        }
        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitComment(this);
        }
    }

    public class ContinueStatement : Statement
    {
        public ContinueStatement(string filename, int start, int end) : base(filename, start, end) { }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitContinue(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitContinue(this);
        }
    }

    public class DelStatement : Statement
    {
        public DelStatement(Exp e, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.Expressions = e;
        }

        public Exp Expressions;

        public override void Accept(IStatementVisitor v)
        {
            v.VisitDel(this);
        }
        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitDel(this);
        }
    }

    public class ExecStatement : Statement
    {
        public ExecStatement(Exp code, Exp globals, Exp locals, string filename, int pos, int end) : base(filename, pos, end)
        {
            this.code = code;
            this.globals = globals;
            this.locals = locals;
        }

        public Exp code;
        public Exp globals;
        public Exp locals;

        public override void Accept(IStatementVisitor v)
        {
            v.VisitExec(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitExec(this);
        }
    }

    public class ExpStatement : Statement
    {
        public readonly Exp Expression;

        public ExpStatement(Exp e, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.Expression = e;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitExp(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitExp(this);
        }
    }

    public class ForStatement : Statement
    {
        public readonly Exp exprs;     // var or vars
        public readonly Exp tests;     // iterator
        public readonly SuiteStatement Body;
        public readonly SuiteStatement Else;

        public ForStatement(
            Exp exprs,
            Exp tests,
            SuiteStatement body,
            SuiteStatement orelse,
            string filename,
            int start, 
            int end) 
            : base(filename, start, end)
        {
            this.exprs = exprs;
            this.tests = tests;
            this.Body = body;
            this.Else = orelse;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitFor(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitFor(this);
        }
    }

    public class FromStatement : Statement
    {
        public readonly DottedName DottedName;
        public readonly List<AliasedName> AliasedNames;

        public FromStatement(DottedName name, List<AliasedName> aliasedNames, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.DottedName = name;
            this.AliasedNames = aliasedNames ?? throw new ArgumentNullException(nameof(aliasedNames));
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitFrom(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitFrom(this);
        }

        internal bool isImportStar()
        {
            return AliasedNames.Count == 0;
        }
    }

    public class GlobalStatement : Statement
    {
        public List<Identifier> names;

        public GlobalStatement(List<Identifier> names, string filename, int start, int end) : base(filename, start, end)
        {
            this.names = names;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitGlobal(this);
        }
        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitGlobal(this);
        }
    }

    public class IfStatement : Statement
    {
        public readonly Exp Test;
        public readonly SuiteStatement Then;
        public readonly SuiteStatement Else;

        public IfStatement(
            Exp test,
            SuiteStatement then,
            SuiteStatement orelse,
            string filename, int start, int end) 
            : base(filename, start, end)
        {
            this.Test = test;
            this.Then = then;
            this.Else = orelse;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitIf(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitIf(this);
        }
    }

    public class PassStatement : Statement
    {
        public PassStatement(string filename, int start, int end) : base(filename, start, end) { }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitPass(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitPass(this);
        }
    }

    public class RaiseStatement : Statement
    {
        public RaiseStatement(Exp exToRaise, Exp exOriginal, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.exToRaise = exToRaise;
            this.exOriginal = exOriginal;
        }

        public Exp exToRaise;
        public Exp exOriginal;
        public Exp traceback;

        public override void Accept(IStatementVisitor v)
        {
            v.VisitRaise(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitRaise(this);
        }
    }

    public class ReturnStatement : Statement
    {
        public readonly Exp Expression;

        public ReturnStatement(string filename, int pos, int end) : base(filename, pos, end)
        {
            Expression = null;
        }

        public ReturnStatement(Exp e, string filename, int pos, int end) : base(filename, pos, end) { Expression = e; }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitReturn(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitReturn(this);
        }
    }

    public class YieldStatement : Statement
    {
        public YieldStatement(Exp exp, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.Expression = exp;
        }

        public Exp Expression;

        public override void Accept(IStatementVisitor v)
        {
            v.VisitYield(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitYield(this);
        }
    }

    public class PrintStatement : Statement
    {
        public PrintStatement(Exp outputStream, List<Argument> args, bool trailingComma, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.outputStream = outputStream;
            this.args = args;
            this.trailingComma = trailingComma;
        }

        public Exp outputStream;
        public List<Argument> args;
        public bool trailingComma;

        public override void Accept(IStatementVisitor v)
        {
            v.VisitPrint(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitPrint(this);
        }
    }

    public class ImportStatement : Statement
    {
        public readonly List<AliasedName> names;

        public ImportStatement(List<AliasedName> names, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            // TODO: Complete member initialization
            this.names = names;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitImport(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitImport(this);
        }
    }

    public class NonlocalStatement : Statement
    {
        public List<Identifier> names;

        public NonlocalStatement(List<Identifier> names, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.names = names;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitNonLocal(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitNonLocal(this);
        }
    }

    public class SuiteStatement : Statement
    {
        public readonly List<Statement> stmts;

        public SuiteStatement(List<Statement> stmts, string filename, int pos, int end) : base(filename, pos, end)
        {
            this.stmts = stmts;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitSuite(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitSuite(this);
        }
    }

    public class TryStatement : Statement
    {
        public SuiteStatement body;
        public List<ExceptHandler> exHandlers;
        public Statement elseHandler;
        public Statement finallyHandler;

        public TryStatement(
            SuiteStatement body,
            List<ExceptHandler> exHandlers, 
            Statement elseHandler, 
            Statement finallyHandler,
            string filename,
            int start,
            int end)
            : base(filename, start, end)
        {
            this.body = body;
            this.exHandlers = exHandlers;
            this.elseHandler = elseHandler;
            this.finallyHandler = finallyHandler;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitTry(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitTry(this);
        }
    }

    public class WithStatement : Statement
    {
        public List<WithItem> items;
        public SuiteStatement body;

        public WithStatement(List<WithItem> ws, SuiteStatement s, string filename, int pos, int end)
            : base(filename, pos, end)
        {
            this.items = ws;
            this.body = s;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitWith(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitWith(this);
        }
    }

    public class WhileStatement : Statement
    {
        public SuiteStatement Body;
        public Exp Test;
        public SuiteStatement Else;

        public WhileStatement(string filename, int start, int end) : base(filename, start, end) { }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitWhile(this);
        }
        
        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitWhile(this);
        }
    }
}
