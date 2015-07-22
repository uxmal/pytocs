namespace Pytocs.Types
{
    using Analyzer = Pytocs.TypeInference.Analyzer;

    public class StrType : DataType
    {
        public string value;

        public StrType(string value)
        {
            this.value = value;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitStr(this);
        }

        public override bool Equals(object other)
        {
            return (other is StrType);
        }

        public override int GetHashCode()
        {
            return "StrType".GetHashCode();
        }
    }
}
