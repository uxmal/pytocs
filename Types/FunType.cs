using Pytocs.TypeInference;
using Pytocs.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Types
{
    public class FunType : DataType
    {
        public IDictionary<DataType, DataType> arrows = new Dictionary<DataType, DataType>();
        public FunctionDef Definition;
        public Lambda lambda;
        public ClassType Class = null;
        public State env;
        public List<DataType> defaultTypes;       // types for default parameters (evaluated at def time)

        public FunType()
        {
            this.Class = null;
        }

        public FunType(FunctionDef func, State env)
        {
            this.Definition = func;
            this.env = env;
        }

        public FunType(Lambda lambda, State env)
        {
            this.lambda = lambda;
            this.env = env;
        }

        public FunType(DataType from, DataType to)
        {
            addMapping(from, to);
        }

        public DataType SelfType { get; set; }                 // self's type for calls

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitFun(this);
        }

        public void addMapping(DataType from, DataType to)
        {
            if (from is TupleType)
            {
                from = simplifySelf((TupleType) from);
            }

            if (arrows.Count < 5)
            {
                arrows[from] = to;
                IDictionary<DataType, DataType> oldArrows = arrows;
                arrows = compressArrows(arrows);

                if (ToString().Length > 900)
                {
                    arrows = oldArrows;
                }
            }
        }

        public DataType getMapping(DataType from)
        {
            DataType to;
            return arrows.TryGetValue(from, out to)
                ? to
                : null;
        }

        public DataType getReturnType()
        {
            if (arrows.Count != 0)
            {
                return arrows.Values.First();
            }
            else
            {
                return DataType.Unknown;
            }
        }

        public void setDefaultTypes(List<DataType> defaultTypes)
        {
            this.defaultTypes = defaultTypes;
        }

        public override bool Equals(object other)
        {
            if (other is FunType)
            {
                FunType fo = (FunType) other;
                return fo.Table.Path.Equals(Table.Path) || object.ReferenceEquals(this , other);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return "FunType".GetHashCode();
        }

        private bool subsumed(DataType type1, DataType type2)
        {
            return subsumedInner(type1, type2, new TypeStack());
        }

        private bool subsumedInner(DataType type1, DataType type2, TypeStack typeStack)
        {
            if (typeStack.contains(type1, type2))
            {
                return true;
            }

            if (type1.isUnknownType() || type1 == DataType.None || type1.Equals(type2))
            {
                return true;
            }

            if (type1 is TupleType && type2 is TupleType)
            {
                List<DataType> elems1 = ((TupleType) type1).eltTypes;
                List<DataType> elems2 = ((TupleType) type2).eltTypes;

                if (elems1.Count == elems2.Count)
                {
                    typeStack.push(type1, type2);
                    for (int i = 0; i < elems1.Count; i++)
                    {
                        if (!subsumedInner(elems1[i], elems2[i], typeStack))
                        {
                            typeStack.pop(type1, type2);
                            return false;
                        }
                    }
                }
                return true;
            }

            if (type1 is ListType && type2 is ListType)
            {
                return subsumedInner(((ListType) type1).toTupleType(), ((ListType) type2).toTupleType(), typeStack);
            }
            return false;
        }

        private IDictionary<DataType, DataType> compressArrows(IDictionary<DataType, DataType> arrows)
        {
            IDictionary<DataType, DataType> ret = new Dictionary<DataType, DataType>();
            foreach (var e1 in arrows)
            {
                bool fSubsumed = false;
                foreach (var e2 in arrows)
                {
                    if (e1.Key != e2.Key && subsumed(e1.Key, e2.Key))
                    {
                        fSubsumed = true;
                        break;
                    }
                }
                if (!fSubsumed)
                {
                    ret[e1.Key] = e1.Value;
                }
            }
            return ret;
        }

        /// <summary>
        /// If the self type is set, use the self type in the display
        /// This is for display purpose only, it may not be logically
        /// correct wrt some pathological programs
        /// </summary>
        private TupleType simplifySelf(TupleType from)
        {
            TupleType simplified = new TupleType();     //$NO regs
            if (from.eltTypes.Count > 0)
            {
                if (Class != null)
                {
                    simplified.add(Class.getCanon());
                }
                else
                {
                    simplified.add(from.get(0));
                }
            }

            for (int i = 1; i < from.eltTypes.Count; i++)
            {
                simplified.add(from.get(i));
            }
            return simplified;
        }


    }
}