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

namespace Pytocs.Core.Types
{
    public interface IDataTypeVisitor<T>
    {
        T VisitAwaitable(AwaitableType awaitable);
        T VisitBool(BoolType b);
        T VisitClass(ClassType c);
        T VisitComplex(ComplexType c);
        T VisitDict(DictType d);
        T VisitFloat(FloatType f);
        T VisitFun(FunType f);
        T VisitInstance(InstanceType i);
        T VisitInt(IntType i);
        T VisitIterable(IterableType i);
        T VisitList(ListType l);
        T VisitModule(ModuleType m);
        T VisitSet(SetType s);
        T VisitStr(StrType s);
        T VisitSymbol(SymbolType s);
        T VisitTuple(TupleType t);
        T VisitUnion(UnionType u);
    }
}
