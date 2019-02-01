using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Types
{
    public class SetType : DataType
    {
        public SetType(DataType elementType)
        {
            this.ElementType = elementType;
        }

        public DataType ElementType { get; private set; }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitSet(this);
        }
    }
}
