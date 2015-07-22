using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.CodeModel
{
    public interface ICodeExpressionVisitor
    {
        void VisitApplication(CodeApplicationExpression app);
        void VisitArrayIndexer(CodeArrayIndexerExpression aref);
        void VisitArrayInitializer(CodeArrayCreateExpression arr);
        void VisitBinary(CodeBinaryOperatorExpression bin);
        void VisitFieldReference(CodeFieldReferenceExpression field);
        void VisitInitializer(CodeInitializerExpression i);
        void VisitLambda(CodeLambdaExpression l);
        void VisitMethodReference(CodeMethodReferenceExpression m);
        void VisitNamedArgument(CodeNamedArgument arg);
        void VisitObjectCreation(CodeObjectCreateExpression c);
        void VisitParameterDeclaration(CodeParameterDeclarationExpression param);
        void VisitPrimitive(CodePrimitiveExpression p);
        void VisitThisReference(CodeThisReferenceExpression t);
        void VisitTypeReference(CodeTypeReferenceExpression t);
        void VisitUnary(CodeUnaryOperatorExpression u);
        void VisitVariableReference(CodeVariableReferenceExpression var);

        void VisitCondition(CodeConditionExpression codeConditionExpression);
    }

    public interface ICodeExpressionVisitor<T>
    {
        T VisitApplication(CodeApplicationExpression app);
        T VisitArrayIndexer(CodeArrayIndexerExpression aref);
        T VisitArrayInitializer(CodeArrayCreateExpression arr);
        T VisitBinary(CodeBinaryOperatorExpression bin);
        T VisitCondition(CodeConditionExpression codeConditionExpression);
        T VisitFieldReference(CodeFieldReferenceExpression field);
        T VisitInitializer(CodeInitializerExpression i);
        T VisitLambda(CodeLambdaExpression l);
        T VisitMethodReference(CodeMethodReferenceExpression m);
        T VisitNamedArgument(CodeNamedArgument arg);
        T VisitObjectCreation(CodeObjectCreateExpression c);
        T VisitParameterDeclaration(CodeParameterDeclarationExpression param);
        T VisitPrimitive(CodePrimitiveExpression p);
        T VisitThisReference(CodeThisReferenceExpression t);
        T VisitTypeReference(CodeTypeReferenceExpression t);
        T VisitUnary(CodeUnaryOperatorExpression u);
        T VisitVariableReference(CodeVariableReferenceExpression var);

    }
}
