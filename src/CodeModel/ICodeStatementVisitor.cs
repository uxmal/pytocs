using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
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
