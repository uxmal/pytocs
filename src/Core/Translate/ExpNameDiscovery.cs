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

using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate
{
    public class ExpNameDiscovery : IExpVisitor
    {
        private SymbolTable syms;

        public ExpNameDiscovery(SymbolTable syms)
        {
            this.syms = syms;
        }

        public void VisitApplication(Application appl)
        {
            throw new NotImplementedException();
        }

        public void VisitArrayRef(ArrayRef arrayRef)
        {
            throw new NotImplementedException();
        }

        public void VisitAssignExp(AssignExp assignExp)
        {
            throw new NotImplementedException();
        }

        public void VisitAssignmentExp(AssignmentExp aExp)
        {
            throw new NotImplementedException();
        }

        public void VisitAwait(AwaitExp awaitExp)
        {
            throw new NotImplementedException();
        }

        public void VisitBigLiteral(BigLiteral bigLiteral)
        {
            throw new NotImplementedException();
        }

        public void VisitBinExp(BinExp bin)
        {
            throw new NotImplementedException();
        }

        public void VisitBooleanLiteral(BooleanLiteral b)
        {
            throw new NotImplementedException();
        }

        public void VisitCompFor(CompFor f)
        {
            throw new NotImplementedException();
        }

        public void VisitCompIf(CompIf i)
        {
            throw new NotImplementedException();
        }

        public void VisitDottedName(DottedName d)
        {
            throw new NotImplementedException();
        }

        public void VisitEllipsis(Ellipsis e)
        {
            throw new NotImplementedException();
        }

        public void VisitExpList(ExpList list)
        {
            throw new NotImplementedException();
        }

        public void VisitFieldAccess(AttributeAccess acc)
        {
            throw new NotImplementedException();
        }

        public void VisitGeneratorExp(GeneratorExp exp)
        {
            throw new NotImplementedException();
        }

        public void VisitIdentifier(Identifier id)
        {
            syms.Reference(id.Name);
        }

        public void VisitImaginaryLiteral(ImaginaryLiteral im)
        {
            throw new NotImplementedException();
        }

        public void VisitIntLiteral(IntLiteral s)
        {
            throw new NotImplementedException();
        }

        public void VisitLambda(Lambda lambda)
        {
            throw new NotImplementedException();
        }

        public void VisitListComprehension(ListComprehension lc)
        {
            throw new NotImplementedException();
        }

        public void VisitList(PyList l)
        {
            throw new NotImplementedException();
        }

        public void VisitLongLiteral(LongLiteral l)
        {
            throw new NotImplementedException();
        }

        public void VisitNoneExp()
        {
            throw new NotImplementedException();
        }

        public void VisitRealLiteral(RealLiteral r)
        {
            throw new NotImplementedException();
        }

        public void VisitSet(PySet setDisplay)
        {
            throw new NotImplementedException();
        }

        public void VisitStarExp(StarExp e)
        {
            throw new NotImplementedException();
        }

        public void VisitBytes(Bytes b)
        {
            throw new NotImplementedException();
        }

        public void VisitStr(Str s)
        {
            throw new NotImplementedException();
        }

        public void VisitTest(TestExp tuple)
        {
            throw new NotImplementedException();
        }

        public void VisitTuple(PyTuple tuple)
        {
            throw new NotImplementedException();
        }

        public void VisitUnary(UnaryExp u)
        {
            throw new NotImplementedException();
        }

        public void VisitYieldExp(YieldExp yieldExp)
        {
            throw new NotImplementedException();
        }

        public void VisitYieldFromExp(YieldFromExp yieldExp)
        {
            throw new NotImplementedException();
        }

        public void VisitAliasedExp(AliasedExp aliasedExp)
        {
            throw new NotImplementedException();
        }

        public void VisitSetComprehension(SetComprehension setComprehension)
        {
            throw new NotImplementedException();
        }

        public void VisitSlice(Slice slice)
        {
            throw new NotImplementedException();
        }

        public void VisitDictComprehension(DictComprehension dc)
        {
            throw new NotImplementedException();
        }

        public void VisitDictInitializer(DictInitializer di)
        {
            throw new NotImplementedException();
        }

        public void VisitIterableUnpacker(IterableUnpacker unpacker)
        {
            throw new NotImplementedException();
        }
    }
}
