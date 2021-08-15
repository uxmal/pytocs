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
using System.Text;

namespace Pytocs.Core.Syntax
{
    public interface IStatementVisitor
    {
        void VisitAsync(AsyncStatement a);
        void VisitAssert(AssertStatement a);
        void VisitBreak(BreakStatement b);
        void VisitClass(ClassDef c);
        void VisitComment(CommentStatement c);
        void VisitContinue(ContinueStatement c);
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
        T VisitAsync(AsyncStatement a);
        T VisitBreak(BreakStatement b);
        T VisitClass(ClassDef c);
        T VisitComment(CommentStatement c);
        T VisitContinue(ContinueStatement c);
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
