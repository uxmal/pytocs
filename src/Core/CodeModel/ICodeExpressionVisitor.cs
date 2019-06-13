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
using System.Linq;
using System.Text;

namespace Pytocs.Core.CodeModel
{
    public interface ICodeExpressionVisitor
    {
        void VisitApplication(CodeApplicationExpression app);
        void VisitArrayIndexer(CodeArrayIndexerExpression aref);
        void VisitArrayInitializer(CodeArrayCreateExpression arr);
        void VisitAwait(CodeAwaitExpression awaitExp);
        void VisitBinary(CodeBinaryOperatorExpression bin);
        void VisitCollectionInitializer(CodeCollectionInitializer i);
        void VisitCondition(CodeConditionExpression codeConditionExpression);
        void VisitFieldReference(CodeFieldReferenceExpression field);
        void VisitLambda(CodeLambdaExpression l);
        void VisitMethodReference(CodeMethodReferenceExpression m);
        void VisitNamedArgument(CodeNamedArgument arg);
        void VisitObjectCreation(CodeObjectCreateExpression c);
        void VisitObjectInitializer(CodeObjectInitializer i);
        void VisitParameterDeclaration(CodeParameterDeclarationExpression param);
        void VisitPrimitive(CodePrimitiveExpression p);
        void VisitQueryExpression(CodeQueryExpression q);

        void VisitThisReference(CodeThisReferenceExpression t);
        void VisitTypeReference(CodeTypeReferenceExpression t);
        void VisitUnary(CodeUnaryOperatorExpression u);
        void VisitValueTuple(CodeValueTupleExpression codeValueTupleExpression);
        void VisitVariableReference(CodeVariableReferenceExpression var);

    }

    public interface ICodeExpressionVisitor<T>
    {
        T VisitApplication(CodeApplicationExpression app);
        T VisitArrayIndexer(CodeArrayIndexerExpression aref);
        T VisitArrayInitializer(CodeArrayCreateExpression arr);
        T VisitAwait(CodeAwaitExpression codeAwaitExpression);
        T VisitBinary(CodeBinaryOperatorExpression bin);
        T VisitCollectionInitializer(CodeCollectionInitializer i);
        T VisitCondition(CodeConditionExpression codeConditionExpression);
        T VisitFieldReference(CodeFieldReferenceExpression field);
        T VisitLambda(CodeLambdaExpression l);
        T VisitMethodReference(CodeMethodReferenceExpression m);
        T VisitNamedArgument(CodeNamedArgument arg);
        T VisitObjectCreation(CodeObjectCreateExpression c);
        T VisitObjectInitializer(CodeObjectInitializer i);
        T VisitParameterDeclaration(CodeParameterDeclarationExpression param);
        T VisitPrimitive(CodePrimitiveExpression p);
        T VisitQueryExpression(CodeQueryExpression q);
        T VisitThisReference(CodeThisReferenceExpression t);
        T VisitTypeReference(CodeTypeReferenceExpression t);
        T VisitUnary(CodeUnaryOperatorExpression u);
        T VisitValueTuple(CodeValueTupleExpression codeValueTupleExpression);
        T VisitVariableReference(CodeVariableReferenceExpression var);

    }
}
