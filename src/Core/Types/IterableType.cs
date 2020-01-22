namespace Pytocs.Core.Types
{
    public class IterableType : DataType
    {
        public DataType ElementType;

        public IterableType(DataType elemType)
        {
            ElementType = elemType;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitIterable(this);
        }
    }
}