namespace Pytocs.Core.Types
{
    public class SetType : DataType
    {
        public SetType(DataType elementType)
        {
            ElementType = elementType;
        }

        public DataType ElementType { get; }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitSet(this);
        }
    }
}