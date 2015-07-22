using System;
using System.Collections.Generic;
using System.Text;
using Analyzer = Pytocs.TypeInference.Analyzer;

namespace Pytocs.Types
{
    public class ListType : DataType
    {
        public DataType eltType;
        public List<DataType> positional = new List<DataType>();
        public List<object> values = new List<object>();

        public ListType()
            : this(DataType.Unknown)
        {
        }

        public ListType(DataType elt0)
        {
            eltType = elt0;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitList(this);
        }

        public void setElementType(DataType eltType)
        {
            this.eltType = eltType;
        }

        public void add(DataType another)
        {
            eltType = UnionType.union(eltType, another);
            positional.Add(another);
        }


        public void addValue(object v)
        {
            values.Add(v);
        }


        public DataType get(int i)
        {
            return positional[i];
        }

        public TupleType toTupleType(int n)
        {
            TupleType ret = new TupleType();    //$ no regs
            for (int i = 0; i < n; i++)
            {
                ret.add(eltType);
            }
            return ret;
        }

        public TupleType toTupleType()
        {
            return new TupleType(positional);
        }

        public override bool Equals(object other)
        {
            if (typeStack.contains(this, other))
            {
                return true;
            }
            else if (other is ListType)
            {
                ListType co = (ListType) other;
                typeStack.push(this, other);
                bool ret = co.eltType.Equals(eltType);
                typeStack.pop(this, other);
                return ret;
            }
            else
            {
                return false;
            }
        }


        public override int GetHashCode()
        {
            return "ListType".GetHashCode();
        }

    }
}