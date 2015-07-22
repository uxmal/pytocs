namespace Pytocs.Types
{
    public class ComplexType : DataType
    {
        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitComplex(this);
        }

        public override bool Equals(object other)
        {
            return other is ComplexType;
        }

        public override int GetHashCode()
        {
            return "ComplexType".GetHashCode();
        }
    }
}
