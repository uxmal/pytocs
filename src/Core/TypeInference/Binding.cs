#region License
//  Copyright 2015-2018 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pytocs.Core.Types;

namespace Pytocs.Core.TypeInference
{
    /// <summary>
    /// A binding associates a type with an entity.
    /// </summary>
    public class Binding : IComparable<Binding>
    {
        public string name;             // unqualified name
        public Node node;               // Thing that has the type.
        public string qname;            // qualified name
        public DataType type;           // inferred type

        public BindingKind kind;        // name usage context

        /// <summary>
        /// The places where this binding is referenced.
        /// </summary>
        public ISet<Node> References { get; }

        // fields from Def
        public int start = -1;
        public int end = -1;
        public int bodyStart = -1;
        public int bodyEnd = -1;
        public string fileOrUrl;

        public Binding(string id, Node node, DataType type, BindingKind kind)
        {
            this.name = id;
            this.qname = type.Table.Path;
            this.type = type;
            this.kind = kind;
            this.node = node;
            this.References = new HashSet<Node>();

            if (node is Url u)
            {
                string url = u.url;
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
                if (node is Identifier idNode)
                    name = node.Name;
            }
            SetLocationInfo(node);
        }

        private void SetLocationInfo(Node node)
        {
            this.start = node.Start;
            this.end = node.End;

            Node parent = node.Parent;
            if ((parent is FunctionDef def && def.name == node) ||
                    (parent is ClassDef cldef && cldef.name == node))
            {
                this.bodyStart = parent.Start;
                this.bodyEnd = parent.End;
                return;
            }
            if (node is Module modNode)
            {
                name = modNode.Name;
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
            if (parent is Pytocs.Core.Syntax.FunctionDef funcDef && funcDef.name == node)
                return parent.GetDocString();
            else
                return node.GetDocString();
        }

        public void AddReference(Node node)
        {
            References.Add(node);
        }

        // merge one more type into the type
        // used by stateful assignments which we can't track down the control flow
        public void AddType(DataType t)
        {
            type = UnionType.Union(type, t);
        }

        public void SetType(DataType type)
        {
            this.type = type;
        }

        // True if this is a static attribute or method.
        public bool IsStatic { get; set; }

        // True if auto-generated bindings
        public bool IsSynthetic { get; set; }

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
                return file ?? "<built-in module>";
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
            if (References.Count > 10)
            {
                sb.Append(References.First());
                sb.AppendFormat(", ...({0} more)]", References.Count - 1);
            }
            else
            {
                var sep = "";
                foreach (var r in References)
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
            if (!(obj is Binding b))
                return false;

            return (Object.ReferenceEquals(node, b.node) &&
                    Object.Equals(fileOrUrl, b.fileOrUrl));
        }

        public override int GetHashCode()
        {
            return node.GetHashCode();
        }
    }

    public enum BindingKind
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
}