using Pytocs.Core.Syntax;

namespace Pytocs.Core.Translate
{
    public class PropertyDefinition
    {
        public Statement Getter;
        public Decorator GetterDecoration;
        public bool IsTranslated;
        public string Name;
        public Statement Setter;
        public Decorator SetterDecoration;

        public PropertyDefinition(string name)
        {
            Name = name;
        }
    }
}