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
using System.Threading.Tasks;

namespace Pytocs.Core.Syntax
{
    public interface IExpVisitor
    {
        void VisitAliasedExp(AliasedExp aliasedExp);
        void VisitApplication(Application appl);
        void VisitArrayRef(ArrayRef arrayRef);
        void VisitAssignExp(AssignExp assignExp);
        void VisitAwait(AwaitExp awaitExp);
        void VisitBigLiteral(BigLiteral bigLiteral);
        void VisitBinExp(BinExp bin);
        void VisitBooleanLiteral(BooleanLiteral b);
        void VisitBytes(Bytes bytes);
        void VisitDictComprehension(DictComprehension dc);
        void VisitDictInitializer(DictInitializer di);
        void VisitDottedName(DottedName dottedName);
        void VisitEllipsis(Ellipsis e);
        void VisitExpList(ExpList list);
        void VisitFieldAccess(AttributeAccess acc);
        void VisitGeneratorExp(GeneratorExp generatorExp);
        void VisitIdentifier(Identifier id);
        void VisitImaginaryLiteral(ImaginaryLiteral im);
        void VisitIntLiteral(IntLiteral s);
        void VisitLambda(Lambda lambda);
        void VisitListComprehension(ListComprehension lc);
        void VisitList(PyList l);
        void VisitLongLiteral(LongLiteral l);
        void VisitNoneExp();
        void VisitRealLiteral(RealLiteral r);
        void VisitSetComprehension(SetComprehension setComprehension);
        void VisitSet(PySet setDisplay);
        void VisitSlice(Slice slice);
        void VisitStarExp(StarExp starExp);
        void VisitStr(Str s);
        void VisitTest(TestExp test);
        void VisitTuple(PyTuple tuple);
        void VisitUnary(UnaryExp u);
        void VisitYieldExp(YieldExp yieldExp);
        void VisitYieldFromExp(YieldFromExp yieldExp);
        void VisitCompFor(CompFor compFor);
        void VisitCompIf(CompIf compIf);
    }

    public interface IExpVisitor<T>
    {
        T VisitAliasedExp(AliasedExp aliasedExp);
        T VisitApplication(Application appl);
        T VisitArrayRef(ArrayRef arrayRef);
        T VisitAssignExp(AssignExp assignExp);
        T VisitAwait(AwaitExp awaitExp);
        T VisitBigLiteral(BigLiteral bigLiteral);
        T VisitBinExp(BinExp bin);
        T VisitBooleanLiteral(BooleanLiteral b);
        T VisitBytes(Bytes bytes);
        T VisitCompFor(CompFor compFor);
        T VisitCompIf(CompIf compIf);
        T VisitDictInitializer(DictInitializer di);
        T VisitDictComprehension(DictComprehension dc);
        T VisitDottedName(DottedName dottedName);
        T VisitEllipsis(Ellipsis e);
        T VisitExpList(ExpList list);
        T VisitFieldAccess(AttributeAccess acc);
        T VisitGeneratorExp(GeneratorExp generatorExp);
        T VisitIdentifier(Identifier id);
        T VisitImaginary(ImaginaryLiteral im);
        T VisitIntLiteral(IntLiteral s);
        T VisitLambda(Lambda lambda);
        T VisitListComprehension(ListComprehension lc);
        T VisitList(PyList l);
        T VisitLongLiteral(LongLiteral l);
        T VisitNoneExp();
        T VisitRealLiteral(RealLiteral r);
        T VisitSet(PySet setDisplay);
        T VisitSetComprehension(SetComprehension setComprehension);
        T VisitSlice(Slice slice);
        T VisitStarExp(StarExp starExp);
        T VisitStr(Str s);
        T VisitTest(TestExp test);
        T VisitTuple(PyTuple tuple);
        T VisitUnary(UnaryExp u);
        T VisitYieldExp(YieldExp yieldExp);
        T VisitYieldFromExp(YieldFromExp yieldExp);
    }
}
