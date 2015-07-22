using Pytocs.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    public class StatementNameDiscovery : IStatementVisitor<SymbolTable>
    {
        public SymbolTable VisitAssert(AssertStatement a)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitBreak(BreakStatement b)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitClass(ClassDef c)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitComment(CommentStatement c)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitContinue(ContinueStatement c)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitDecorated(Decorated d)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitDel(DelStatement d)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitExec(ExecStatement exec)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitExp(ExpStatement e)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitFor(ForStatement f)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitFrom(FromStatement f)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitFunctionDef(FunctionDef f)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitGlobal(GlobalStatement g)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitIf(IfStatement i)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitImport(ImportStatement i)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitNonLocal(NonlocalStatement n)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitPass(PassStatement p)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitPrint(PrintStatement p)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitRaise(RaiseStatement r)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitReturn(ReturnStatement r)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitSuite(SuiteStatement s)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitTry(TryStatement t)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitWhile(WhileStatement w)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitWith(WithStatement w)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitYield(YieldStatement y)
        {
            throw new NotImplementedException();
        }
    }
}
