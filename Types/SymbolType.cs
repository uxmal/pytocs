namespace Pytocs.Types
{
    public class SymbolType : DataType
    {
        public string name;

        public SymbolType(string name)
        {
            this.name = name;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitSymbol(this);
        }

        public override bool Equals(object other)
        {
            if (other is SymbolType)
            {
                return this.name.Equals(((SymbolType) other).name);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}