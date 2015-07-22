namespace Pytocs.Types
{
    public class FloatType : DataType
    {
        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitFloat(this);
        }

        public override bool Equals(object other)
        {
            return other is FloatType;
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }
    }
}