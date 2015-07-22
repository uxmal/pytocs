using System.Collections.Generic;
using State = Pytocs.TypeInference.State;

namespace Pytocs.Types
{
    public class InstanceType : DataType
    {
        public DataType classType;

        public InstanceType(DataType c)
        {
            Table.setStateType(State.StateType.INSTANCE);
            Table.addSuper(c.Table);
            Table.Path = c.Table.Path;
            classType = c;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitInstance(this);
        }

        public override bool Equals(object other)
        {
            if (other is InstanceType)
            {
                return classType.Equals(((InstanceType) other).classType);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return classType.GetHashCode();
        }
    }
}