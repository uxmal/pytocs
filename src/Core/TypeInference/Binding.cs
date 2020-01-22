#region License

//  Copyright 2015-2020 John Källén
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

#endregion License

using Pytocs.Core.Syntax;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Core.TypeInference
{
    /// <summary>
    ///     A binding associates a type with an entity.
    /// </summary>
    public class Binding : IComparable<Binding>
    {
        public readonly BindingKind Kind; // name usage context

        public readonly string Name; // unqualified name
        public readonly Node Node; // entity that has the type.
        public int bodyEnd = -1;
        public int bodyStart = -1;

        public int end = -1;
        public string fileOrUrl;
        public string QName; // qualified name

        // fields from Def
        public int start = -1;

        public DataType Type; // inferred type

        public Binding(string id, Node node, DataType type, BindingKind kind)
        {
            Name = id;
            QName = type.Table.Path;
            Type = type;
            Kind = kind;
            Node = node;
            References = new HashSet<Node>();

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
                {
                    Name = idNode.Name;
                }
            }

            Name = SetLocationInfo(node);
        }

        /// <summary>
        ///     The places where this binding is referenced.
        /// </summary>
        public ISet<Node> References { get; }

        // True if this is a static attribute or method.
        public bool IsStatic { get; set; }

        // True if auto-generated bindings
        public bool IsSynthetic { get; set; }

        /// <summary>
        ///     True if not from a source file.
        /// </summary>
        public bool IsBuiltin { get; set; }

        public string File => IsURL ? null : fileOrUrl;

        public string URL => IsURL ? fileOrUrl : null;

        public bool IsURL => fileOrUrl != null && fileOrUrl.StartsWith("http://");

        /// <summary>
        ///     Bindings can be sorted by their location for outlining purposes.
        /// </summary>
        public int CompareTo(Binding that)
        {
            return start - that.start;
        }

        private string SetLocationInfo(Node node)
        {
            start = node.Start;
            end = node.End;

            Node parent = node.Parent;
            if (parent is FunctionDef def && def.name == node ||
                parent is ClassDef cldef && cldef.name == node)
            {
                bodyStart = parent.Start;
                bodyEnd = parent.End;
                return Name;
            }

            if (node is Module modNode)
            {
                start = 0;
                end = 0;
                bodyStart = node.Start;
                bodyEnd = node.End;
                return modNode.Name;
            }

            bodyStart = node.Start;
            bodyEnd = node.End;
            return Name;
        }

        public Str GetDocString()
        {
            Node parent = Node.Parent;
            if (parent is FunctionDef funcDef && funcDef.name == Node)
            {
                return parent.GetDocString();
            }

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
            Type = type;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
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
                string sep = "";
                foreach (Node r in References)
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
            {
                return false;
            }

            return ReferenceEquals(Node, b.Node) &&
                   Equals(fileOrUrl, b.fileOrUrl);
        }

        public override int GetHashCode()
        {
            return Node.GetHashCode();
        }
    }
}