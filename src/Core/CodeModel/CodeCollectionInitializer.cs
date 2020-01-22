namespace Pytocs.Core.CodeModel
{
    public class CodeCollectionInitializer : CodeInitializer
    {
        public CodeCollectionInitializer(params CodeExpression[] values)
        {
            Values = values;
        }

        public CodeExpression[] Values { get; set; }

        public override void Accept(ICodeExpressionVisitor visitor)
        {
            visitor.VisitCollectionInitializer(this);
        }

        public override T Accept<T>(ICodeExpressionVisitor<T> visitor)
        {
            return visitor.VisitCollectionInitializer(this);
        }
    }
}