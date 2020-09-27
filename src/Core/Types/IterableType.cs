using System;
using System.Collections.Generic;
using System.Text;

namespace Pytocs.Core.Types
{
    public class IterableType : DataType
    {
        public DataType ElementType;

        public IterableType(DataType elemType)
        {
            this.ElementType = elemType;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitIterable(this);
        }

        public override DataType MakeGenericType(params DataType[] typeArguments)
        {
            if (typeArguments.Length != 1)
                throw new ArgumentException("Should only be one type argument.");
            return new IterableType(typeArguments[0]);
        }
    }
}
