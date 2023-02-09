#region License
//  Copyright 2015-2021 John Källén
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
        public Binding(string id, Node node, DataType type, BindingKind kind)
        {
            this.Name = id;
            this.QName = type.Scope.Path ?? "";
            this.Type = type;
            this.Kind = kind;
            this.Node = node;
            this.References = new HashSet<Node>();

            if (node is Url u)
            {
                string url = u.Value;
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
                    Name = idNode.Name;
            }
            Name = SetLocationInfo(node);
        }

        public readonly string Name;            // unqualified name
        public readonly Node Node;              // entity that has the type.
        public readonly BindingKind Kind;       // name usage context
        public string QName;                    // qualified name
        public DataType Type;                   // inferred type

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

        // True if this is a static attribute or method.
        public bool IsStatic { get; set; }

        // True if auto-generated bindings
        public bool IsSynthetic { get; set; }

        /// <summary>
        /// True if not from a source file.
        /// </summary>
        public bool IsBuiltin { get; set; }
        public string? File => IsURL ? null : fileOrUrl;

        public string? URL => IsURL ? fileOrUrl : null;

        public bool IsURL => fileOrUrl != null && fileOrUrl.StartsWith("http://");


        private string SetLocationInfo(Node node)
        {
            this.start = node.Start;
            this.end = node.End;

            Node? parent = node.Parent;
            if ((parent is FunctionDef def && def.name == node) ||
                    (parent is ClassDef cldef && cldef.name == node))
            {
                this.bodyStart = parent.Start;
                this.bodyEnd = parent.End;
                return Name;
            }
            if (node is Module modNode)
            {
                this.start = 0;
                this.end = 0;
                this.bodyStart = node.Start;
                this.bodyEnd = node.End;
                return modNode.Name;
            }
            else
            {
                this.bodyStart = node.Start;
                this.bodyEnd = node.End;
                return Name;
            }
        }

        public Str GetDocString()
        {
            Node? parent = Node.Parent;
            if (parent is FunctionDef funcDef && funcDef.name == Node)
                return funcDef.GetDocString();
            else
                return Node.GetDocString();
        }

        public void AddReference(Node node)
        {
            References.Add(node);
        }

        // merge one more type into the type
        // used by stateful assignments which we can't track down the control flow
        public void AddType(DataType t)
        {
            Type = UnionType.Union(Type, t);
        }

        public void SetType(DataType type)
        {
            this.Type = type;
        }



        /// <summary>
        /// Bindings can be sorted by their location for outlining purposes.
        /// </summary>
        public int CompareTo(Binding? that)
        {
            if (that is null)
                return 1;
            return start - that.start;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(binding");
            sb.Append(":kind=").Append(Kind);
            sb.Append(":node=").Append(Node);
            sb.Append(":type=").Append(Type);
            sb.Append(":qname=").Append(QName);
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

        public override bool Equals(object? obj)
        {
            if (obj is not Binding b)
                return false;

            return (Object.ReferenceEquals(Node, b.Node) &&
                    Object.Equals(fileOrUrl, b.fileOrUrl));
        }

        public override int GetHashCode()
        {
            return Node.GetHashCode();
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