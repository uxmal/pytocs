using State = Pytocs.TypeInference.State;

namespace Pytocs.Types
{
    public class ClassType : DataType
    {
        public string name;
        public InstanceType canon;
        public DataType superclass;

        public ClassType(string name, State parent, string path)
        {
            this.name = name;
            this.Table = new State(parent, State.StateType.CLASS);
            this.Table.Type = this;
            if (parent != null)
            {
                Table.Path = path;
            }
            else
            {
                Table.Path = name;
            }
        }

        public ClassType(string name, State parent, string path, ClassType superClass)
            : this(name, parent, path)
        {
            if (superClass != null)
            {
                addSuper(superClass);
            }
        }

        public override T Accept<T>(IDataTypeVisitor<T> visitor)
        {
            return visitor.VisitClass(this);
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public void addSuper(DataType superclass)
        {
            this.superclass = superclass;
            Table.addSuper(superclass.Table);
        }

        public InstanceType getCanon()
        {
            if (canon == null)
            {
                canon = new InstanceType(this);
            }
            return canon;
        }

        public override bool Equals(object other)
        {
            return object.ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
