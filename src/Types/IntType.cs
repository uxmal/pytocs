namespace Pytocs.Types
{
    public class IntType : DataType
    {
        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitInt(this);
        }

        public override bool Equals(object other)
        {
            return other is IntType;
        }

        public override int GetHashCode()
        {
            return "IntType".GetHashCode();
        }
    }
}