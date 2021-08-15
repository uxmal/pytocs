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

namespace Pytocs.Core.CodeModel
{
    public interface ICodeStatementVisitor
    {
        void VisitAssignment(CodeAssignStatement ass);
        void VisitBreak(CodeBreakStatement b);
        void VisitComment(CodeCommentStatement codeCommentStatement);
        void VisitContinue(CodeContinueStatement c);
        void VisitForeach(CodeForeachStatement f);
        void VisitIf(CodeConditionStatement cond);
        void VisitPostTestLoop(CodePostTestLoopStatement l);
        void VisitPreTestLoop(CodePreTestLoopStatement l);
        void VisitReturn(CodeMethodReturnStatement ret);
        void VisitSideEffect(CodeExpressionStatement side);
        void VisitThrow(CodeThrowExceptionStatement t);
        void VisitTry(CodeTryCatchFinallyStatement t);
        void VisitUsing(CodeUsingStatement u);
        void VisitVariableDeclaration(CodeVariableDeclarationStatement decl);
        void VisitYield(CodeYieldStatement y);
    }

    public interface ICodeStatementVisitor<T>
    {
        T VisitAssignment(CodeAssignStatement ass);
        T VisitBreak(CodeBreakStatement b);
        T VisitComment(CodeCommentStatement codeCommentStatement);
        T VisitContinue(CodeContinueStatement c);
        T VisitForeach(CodeForeachStatement f);
        T VisitIf(CodeConditionStatement cond);
        T VisitLocalFunction(CodeLocalFunction fn);
        T VisitPostTestLoop(CodePostTestLoopStatement l);
        T VisitPreTestLoop(CodePreTestLoopStatement l);
        T VisitReturn(CodeMethodReturnStatement ret);
        T VisitSideEffect(CodeExpressionStatement side);
        T VisitThrow(CodeThrowExceptionStatement t);
        T VisitTry(CodeTryCatchFinallyStatement t);
        T VisitUsing(CodeUsingStatement codeUsingStatement);
        T VisitVariableDeclaration(CodeVariableDeclarationStatement decl);
        T VisitYield(CodeYieldStatement y);
    }
}
