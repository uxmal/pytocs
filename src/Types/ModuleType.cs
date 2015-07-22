using org.yinwang.pysonar;
using State = Pytocs.TypeInference.State;

namespace Pytocs.Types
{
    public class ModuleType : DataType
    {
        public string name;
        public string qname;

        public ModuleType(string name, string file, string qName, State parent)
        {
            this.name = name;
            this.file = file;  // null for builtin modules
            this.qname = qName;
            this.Table = new State(parent, State.StateType.MODULE);
            Table.Path = qname;
            Table.Type = this;
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitModule(this);
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public override int GetHashCode()
        {
            return GetType().Name.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is ModuleType)
            {
                ModuleType co = (ModuleType) other;
                if (file != null)
                {
                    return file.Equals(co.file);
                }
            }
            return object.ReferenceEquals(this, other);
        }
    }
}
