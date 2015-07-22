using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public interface IStatementVisitor
    {
        void VisitAssert(AssertStatement a);
        void VisitBreak(BreakStatement b);
        void VisitClass(ClassDef c);
        void VisitComment(CommentStatement c);
        void VisitContinue(ContinueStatement c);
        void VisitDecorated(Decorated d);
        void VisitDel(DelStatement d);
        void VisitExec(ExecStatement exec);
        void VisitExp(ExpStatement e); 
        void VisitFor(ForStatement f);
        void VisitFrom(FromStatement f);
        void VisitFuncdef(FunctionDef f);
        void VisitGlobal(GlobalStatement g);
        void VisitIf(IfStatement i);
        void VisitImport(ImportStatement i);
        void VisitNonLocal(NonlocalStatement n);
        void VisitPass(PassStatement p);
        void VisitPrint(PrintStatement p);
        void VisitRaise(RaiseStatement r);
        void VisitReturn(ReturnStatement r);
        void VisitSuite(SuiteStatement s);
        void VisitTry(TryStatement t);
        void VisitWhile(WhileStatement w);
        void VisitWith(WithStatement w);
        void VisitYield(YieldStatement y);
    }

    public interface IStatementVisitor<T>
    {
        T VisitAssert(AssertStatement a);
        T VisitBreak(BreakStatement b);
        T VisitClass(ClassDef c);
        T VisitComment(CommentStatement c);
        T VisitContinue(ContinueStatement c);
        T VisitDecorated(Decorated d);
        T VisitDel(DelStatement d);
        T VisitExec(ExecStatement exec);
        T VisitExp(ExpStatement e);
        T VisitFor(ForStatement f);
        T VisitFrom(FromStatement f);
        T VisitFunctionDef(FunctionDef f);
        T VisitGlobal(GlobalStatement g);
        T VisitIf(IfStatement i);
        T VisitImport(ImportStatement i);
        T VisitNonLocal(NonlocalStatement n);
        T VisitPass(PassStatement p);
        T VisitPrint(PrintStatement p);
        T VisitRaise(RaiseStatement r);
        T VisitReturn(ReturnStatement r);
        T VisitSuite(SuiteStatement s);
        T VisitTry(TryStatement t);
        T VisitWhile(WhileStatement w);
        T VisitWith(WithStatement w);
        T VisitYield(YieldStatement y);
    }

}
