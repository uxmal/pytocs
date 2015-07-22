namespace Pytocs.Types
{
    public class BoolType : DataType
    {
        public enum Value
        {
            True,
            False,
            Undecided
        }

        public Value value;

        public BoolType(Value value)
        {
            this.value = value;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitBool(this);
        }

        public void setValue(Value value)
        {
            this.value = value;
        }

        public override bool Equals(object other)
        {
            return (other is BoolType);
        }

        public override int GetHashCode()
        {
            return "BoolType".GetHashCode();
        }
    }
}