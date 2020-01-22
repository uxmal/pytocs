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