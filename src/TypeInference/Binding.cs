using Pytocs.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pytocs.Types;

namespace Pytocs.TypeInference
{
    public class Binding : IComparable<Binding>
    {
        public enum Kind
        {
            ATTRIBUTE,    // attr accessed with "." on some other object
            CLASS,        // class definition
            CONSTRUCTOR,  // __init__ functions in classes
            FUNCTION,     // plain function
            METHOD,       // static or instance method
            MODULE,       // file
            PARAMETER,    // function param
            SCOPE,        // top-level variable ("scope" means we assume it can have attrs)
            VARIABLE      // local variable
        }

        private bool _isStatic = false;         // static fields/methods
        private bool _isSynthetic = false;      // auto-generated bindings

        public string name;     // unqualified name
        public Node node;
        public string qname     // qualified name
        {
            get;
            set;
        }

        public DataType type;   // inferred type
        public Kind kind
        {       // name usage context
            get;
            set;
        }

        /// <summary>
        /// The places where this binding is referenced.
        /// </summary>
        public ISet<Node> refs = new HashSet<Node>();

        // fields from Def
        public int start = -1;
        public int end = -1;
        public int bodyStart = -1;
        public int bodyEnd = -1;
        public string fileOrUrl;

        public Binding(string id, Node node, DataType type, Kind kind)
        {
            this.name = id;
            this.qname = type.Table.Path;
            this.type = type;
            this.kind = kind;
            this.node = node;

            if (node is Url)
            {
                string url = ((Url) node).url;
                if (url.StartsWith("file://"))
                {
                    fileOrUrl = url.Substring("file://".Length);
                }
                else
                {
                    fileOrUrl = url;
                }
            }
            else
            {
                fileOrUrl = node.Filename;
                if (node is Identifier)
                {
                    name = ((Identifier) node).Name;
                }
            }

            initLocationInfo(node);
        }

        private void initLocationInfo(Node node)
        {
            this.start = node.Start;
            this.end = node.End;

            Node parent = node.Parent;
            if ((parent is FunctionDef && ((FunctionDef) parent).name == node) ||
                    (parent is ClassDef && ((ClassDef) parent).name == node))
            {
                this.bodyStart = parent.Start;
                this.bodyEnd = parent.End;
            }
            else if (node is Module)
            {
                name = ((Module) node).Name;
                this.start = 0;
                this.end = 0;
                this.bodyStart = node.Start;
                this.bodyEnd = node.End;
            }
            else
            {
                this.bodyStart = node.Start;
                this.bodyEnd = node.End;
            }
        }

        public Str GetDocString()
        {
            Node parent = node.Parent;
            var funcDef = parent as Pytocs.Syntax.FunctionDef;
            var classDef = parent as Pytocs.Syntax.ClassDef;
            if (funcDef != null && funcDef.name == node)
                return parent.GetDocString();
            else
                return node.GetDocString();
        }

        public void addRef(Node node)
        {
            refs.Add(node);
        }

        // merge one more type into the type
        // used by stateful assignments which we can't track down the control flow
        public void addType(DataType t)
        {
            type = UnionType.union(type, t);
        }

        public void setType(DataType type)
        {
            this.type = type;
        }

        public bool IsStatic
        {
            get { return _isStatic; }
            set { _isStatic = value; }
        }

        public bool IsSynthetic
        {
            get { return _isSynthetic; }
            set { _isSynthetic = value; }
        }

        /// <summary>
        /// True if not from a source file.
        /// </summary>
        public bool IsBuiltin { get; set; }

        public string getFirstFile()
        {
            string file;
            DataType bt = type;
            if (bt is ModuleType)
            {
                file = bt.asModuleType().file;
                return file != null ? file : "<built-in module>";
            }

            file = getFile();
            if (file != null)
            {
                return file;
            }
            return "<built-in binding>";
        }

        public string getFile()
        {
            return isURL() ? null : fileOrUrl;
        }

        public string getURL()
        {
            return isURL() ? fileOrUrl : null;
        }

        public bool isURL()
        {
            return fileOrUrl != null && fileOrUrl.StartsWith("http://");
        }


        /// <summary>
        /// Bindings can be sorted by their location for outlining purposes.
        /// </summary>
        public int CompareTo(Binding o)
        {
            return start - o.start;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(binding");
            sb.Append(":kind=").Append(kind);
            sb.Append(":node=").Append(node);
            sb.Append(":type=").Append(type);
            sb.Append(":qname=").Append(qname);
            sb.Append(":refs=");
            sb.Append("[");
            if (refs.Count > 10)
            {
                sb.Append(refs.First());
                sb.AppendFormat(", ...({0} more)]", refs.Count - 1);
            }
            else
            {
                var sep = "";
                foreach (var r in refs)
                {
                    sb.Append(sep);
                    sep = ",";
                    sb.Append(r);
                }
                sb.Append("]");
            }
            sb.Append(")");
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            Binding b = obj as Binding;
            if (b == null)
                return false;

            return (Object.ReferenceEquals(node, b.node) &&
                    Object.Equals(fileOrUrl, b.fileOrUrl));
        }

        public override int GetHashCode()
        {
            return node.GetHashCode();
        }
    }
}