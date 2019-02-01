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

using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate
{
    public class ClassNameDiscovery : IStatementVisitor<SymbolTable>
    {
        public SymbolTable VisitAssert(AssertStatement a)
        {
            throw new NotImplementedException();
        }

        public SymbolTable VisitAsync(AsyncStatement a)
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
