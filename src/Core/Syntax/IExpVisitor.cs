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
using System.Threading.Tasks;

namespace Pytocs.Core.Syntax
{
    public interface IExpVisitor
    {
        void VisitAliasedExp(AliasedExp aliasedExp);
        void VisitApplication(Application appl);
        void VisitArrayRef(ArrayRef arrayRef);
        void VisitAssignExp(AssignExp assignExp);
        void VisitAssignmentExp(AssignmentExp assignmentExp);
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
        void VisitIterableUnpacker(IterableUnpacker iterableUnpacker);
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
        T VisitAssignmentExp(AssignmentExp assignmentExp);
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
        T VisitIterableUnpacker(IterableUnpacker unpacker);
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

    public interface IExpVisitor<T, C>
    {
        T VisitAliasedExp(AliasedExp aliasedExp, C context);
        T VisitApplication(Application appl, C context);
        T VisitArrayRef(ArrayRef arrayRef, C context);
        T VisitAssignExp(AssignExp assignExp, C context);
        T VisitAssignmentExp(AssignmentExp assignmentExp, C context);
        T VisitAwait(AwaitExp awaitExp, C context);
        T VisitBigLiteral(BigLiteral bigLiteral, C context);
        T VisitBinExp(BinExp bin, C context);
        T VisitBooleanLiteral(BooleanLiteral b, C context);
        T VisitBytes(Bytes bytes, C context);
        T VisitCompFor(CompFor compFor, C context);
        T VisitCompIf(CompIf compIf, C context);
        T VisitDictInitializer(DictInitializer di, C context);
        T VisitDictComprehension(DictComprehension dc, C context);
        T VisitDottedName(DottedName dottedName, C context);
        T VisitEllipsis(Ellipsis e, C context);
        T VisitExpList(ExpList list, C context);
        T VisitFieldAccess(AttributeAccess acc, C context);
        T VisitGeneratorExp(GeneratorExp generatorExp, C context);
        T VisitIdentifier(Identifier id, C context);
        T VisitImaginary(ImaginaryLiteral im, C context);
        T VisitIntLiteral(IntLiteral s, C context);
        T VisitIterableUnpacker(IterableUnpacker unpacker, C context);
        T VisitLambda(Lambda lambda, C context);
        T VisitListComprehension(ListComprehension lc, C context);
        T VisitList(PyList l, C context);
        T VisitLongLiteral(LongLiteral l, C context);
        T VisitNoneExp(C context);
        T VisitRealLiteral(RealLiteral r, C context);
        T VisitSet(PySet setDisplay, C context);
        T VisitSetComprehension(SetComprehension setComprehension, C context);
        T VisitSlice(Slice slice, C context);
        T VisitStarExp(StarExp starExp, C context);
        T VisitStr(Str s, C context);
        T VisitTest(TestExp test, C context);
        T VisitTuple(PyTuple tuple, C context);
        T VisitUnary(UnaryExp u, C context);
        T VisitYieldExp(YieldExp yieldExp, C context);
        T VisitYieldFromExp(YieldFromExp yieldExp, C context);
    }
}
