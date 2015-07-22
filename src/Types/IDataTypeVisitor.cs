using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Types
{
    public interface IDataTypeVisitor<T>
    {
        T VisitBool(BoolType b);
        T VisitClass(ClassType c);
        T VisitComplex(ComplexType c);
        T VisitDict(DictType d);
        T VisitFloat(FloatType f);
        T VisitFun(FunType f);
        T VisitInstance(InstanceType i);
        T VisitInt(IntType i);
        T VisitList(ListType l);
        T VisitModule(ModuleType m);
        T VisitStr(StrType s);
        T VisitSymbol(SymbolType s);
        T VisitTuple(TupleType t);
        T VisitUnion(UnionType u);
    }
}
