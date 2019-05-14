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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pytocs.Core.Types;
using Pytocs.Core.Syntax;

namespace Pytocs.Core.TypeInference
{
    /// <summary>
    /// Implements a scope, which maps names to sets of bindings.
    /// </summary>
    public class State
    {
        public enum StateType
        {
            CLASS,
            INSTANCE,
            FUNCTION,
            MODULE,
            GLOBAL,
            SCOPE
        }

        public IDictionary<string, ISet<Binding>> table = new Dictionary<string, ISet<Binding>>(0);
        public State Parent { get; set; }      // all are non-null except global table
        public State Forwarding { get; set; }  // link to the closest non-class scope, for lifting functions out
        private List<State> supers;
        public ISet<string> globalNames;
        public StateType stateType { get; set; }
        public string Path { get; set; }
        public DataType DataType { get; set; }

        public State(State parent, StateType type)
        {
            this.Parent = parent;
            this.stateType = type;
            this.Path = "";

            if (type == StateType.CLASS)
            {
                this.Forwarding = parent == null ? null : parent.getForwarding();
            }
            else
            {
                this.Forwarding = this;
            }
        }

        public State(State s)
        {
            this.table = new Dictionary<string, ISet<Binding>>(s.table);
            this.Parent = s.Parent;
            this.stateType = s.stateType;
            this.Forwarding = s.Forwarding;
            this.supers = s.supers;
            this.globalNames = s.globalNames;
            this.DataType = s.DataType;
            this.Path = s.Path;
        }

        // erase and overwrite this to s's contents
        public void Overwrite(State s)
        {
            this.table = s.table;
            this.Parent = s.Parent;
            this.stateType = s.stateType;
            this.Forwarding = s.Forwarding;
            this.supers = s.supers;
            this.globalNames = s.globalNames;
            this.DataType = s.DataType;
            this.Path = s.Path;
        }

        public State Clone()
        {
            return new State(this);
        }

        public void Merge(State other)
        {
            foreach (var e2 in other.table)
            {
                ISet<Binding> b1 = table.ContainsKey(e2.Key) ? table[e2.Key] : null;
                ISet<Binding> b2 = e2.Value;

                if (b1 != null && b2 != null)
                {
                    b1.UnionWith(b2);
                }
                else if (b1 == null && b2 != null)
                {
                    table[e2.Key] = b2;
                }
            }
        }

        public static State Merge(State state1, State state2)
        {
            var ret = state1.Clone();
            ret.Merge(state2);
            return ret;
        }

        public State getForwarding()
        {
            if (Forwarding != null)
            {
                return Forwarding;
            }
            else
            {
                return this;
            }
        }

        public void addSuper(State sup)
        {
            if (supers == null)
            {
                supers = new List<State>();
            }
            supers.Add(sup);
        }

        public void setStateType(StateType type)
        {
            this.stateType = type;
        }

        public void AddGlobalName(string name)
        {
            if (globalNames == null)
            {
                globalNames = new HashSet<string>();
            }
            globalNames.Add(name);
        }

        public bool IsGlobalName(string name)
        {
            if (globalNames != null)
            {
                return globalNames.Contains(name);
            }
            else if (Parent != null)
            {
                return Parent.IsGlobalName(name);
            }
            else
            {
                return false;
            }
        }

        public void Remove(string id)
        {
            table.Remove(id);
        }

        public Binding Insert(Analyzer analyzer, string id, Module node, DataType type, BindingKind kind)
        {
            Binding b = analyzer.CreateBinding(id, node, type, kind);
            if (type is ModuleType)
            {
                b.qname = type.asModuleType().qname;
            }
            else
            {
                b.qname = analyzer.ExtendPath(this.Path, id);
            }
            Update(id, b);
            return b;
        }

        public Binding Insert(Analyzer analyzer, string id, Exp node, DataType type, BindingKind kind)
        {
            Binding b = analyzer.CreateBinding(id, node, type, kind);
            if (type is ModuleType)
            {
                b.qname = type.asModuleType().qname;
            }
            else
            {
                b.qname = ExtendPath(analyzer, id);
            }
            Update(id, b);
            return b;
        }

        // directly insert a given binding
        public ISet<Binding> Update(string id, ISet<Binding> bs)
        {
            table[id] = bs;
            return bs;
        }

        public ISet<Binding> Update(string id, Binding b)
        {
            ISet<Binding> bs = new HashSet<Binding> { b };
            table[id] = bs;
            return bs;
        }

        /// <summary>
        /// Look up a name in the current symbol table only. Don't recurse on the
        /// parent table.
        /// </summary>
        public ISet<Binding> LookupLocal(string name)
        {
            if (table.TryGetValue(name, out var bs))
                return bs;
            else 
                return null;
        }

        /// <summary>
        /// Look up a name in the current symbol table.  If not found,
        /// recurse on the parent symbol table.
        /// </summary>
        public ISet<Binding> Lookup(string name)
        {
            ISet<Binding> b = getModuleBindingIfGlobal(name);
            if (b != null)
            {
                return b;
            }
            ISet<Binding> ent = LookupLocal(name);
            if (ent != null)
            {
                return ent;
            }
            else if (Parent != null)
            {
                return Parent.Lookup(name);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Look up a name in the module if it is declared as global, otherwise look
        /// it up locally.
        /// </summary>
        public ISet<Binding> LookupScope(string name)
        {
            ISet<Binding> b = getModuleBindingIfGlobal(name);
            if (b != null)
            {
                return b;
            }
            else
            {
                return LookupLocal(name);
            }
        }


        /**
         * Look up an attribute in the type hierarchy.  Don't look at parent link,
         * because the enclosing scope may not be a super class. The search is
         * "depth first, left to right" as in Python's (old) multiple inheritance
         * rule. The new MRO can be implemented, but will probably not introduce
         * much difference.
         */
        private static ISet<State> looked = new HashSet<State>();    // circularity prevention

        public ISet<Binding> LookupAttribute(string attr)
        {
            if (looked.Contains(this))
            {
                return null;
            }

            ISet<Binding> b = LookupLocal(attr);
            if (b != null)
            {
                return b;
            }
            else
            {
                if (supers != null && supers.Count > 0)
                {
                    looked.Add(this);
                    foreach (State p in supers)
                    {
                        b = p.LookupAttribute(attr);
                        if (b != null)
                        {
                            looked.Remove(this);
                            return b;
                        }
                    }
                    looked.Remove(this);
                }
                return null;
            }
        }

        /// <summary>
        /// Look for a binding named <paramref name="name"/> and if found, return its type.
        /// </summary>
        public DataType LookupType(string name)
        {
            ISet<Binding> bs = Lookup(name);
            if (bs == null)
            {
                return null;
            }
            else
            {
                return MakeUnion(bs);
            }
        }

        /// <summary>
        /// Look for an attribute named <param name="attr" />
        /// and if found, return its type.
        /// </summary>
        public DataType LookupAttributeType(string attr)
        {
            ISet<Binding> bs = LookupAttribute(attr);
            if (bs == null)
            {
                return null;
            }
            else
            {
                return MakeUnion(bs);
            }
        }

        /// <summary>
        /// Create a datatype from the union of the 
        /// types of each binding.
        /// </summary>
        public static DataType MakeUnion(ISet<Binding> bs)
        {
            DataType t = DataType.Unknown;
            foreach (Binding b in bs)
            {
                t = UnionType.Union(t, b.type);
            }
            return t;
        }

        /// <summary>
        /// Find a symbol table of a certain type in the enclosing scopes.
        /// </summary>
        public State getStateOfType(StateType type)
        {
            if (stateType == type)
            {
                return this;
            }
            else if (Parent == null)
            {
                return null;
            }
            else
            {
                return Parent.getStateOfType(type);
            }
        }

        /**
         * Returns the global scope (i.e. the module scope for the current module).
         */
        public State GetGlobalTable()
        {
            State result = getStateOfType(StateType.MODULE);
            Debug.Assert(result != null, "Couldn't find global table.");
            return result;
        }


        /// <summary>
        /// If {@code name} is declared as a global, return the module binding.
        /// </summary>
        private ISet<Binding> getModuleBindingIfGlobal(string name)
        {
            if (IsGlobalName(name))
            {
                State module = GetGlobalTable();
                if (module != this)
                {
                    return module.LookupLocal(name);
                }
            }
            return null;
        }

        public void putAll(State other)
        {
            foreach (var de in other.table)
            {
                table.Add(de.Key, de.Value);
            }
        }

        public IEnumerable<ISet<Binding>> Values
        {
            get { return table.Values; }
        }

        public ICollection<KeyValuePair<string, ISet<Binding>>> entrySet()
        {
            return table;
        }

        public string ExtendPath(Analyzer analyzer, string pathname)
        {
            return analyzer.ExtendPath(this.Path, pathname); 
        }

        public override string ToString()
        {
            return "<State:" + stateType + ":" + table.Keys + ">";
        }

        /// <summary>
        /// Bind a name to this scope, including destructuring assignment.
        /// </summary>
        public void Bind(Analyzer analyzer, Exp target, DataType rvalue, BindingKind kind)
        {
            if (target is Identifier id)
            {
                this.Bind(analyzer, id, rvalue, kind);
            }
            else if (target is PyTuple tup)
            {
                this.Bind(analyzer, tup.values, rvalue, kind);
            }
            else if (target is PyList list)
            {
                this.Bind(analyzer, list.elts, rvalue, kind);
            }
            else if (target is AttributeAccess attr)
            {
                DataType targetType = TransformExp(analyzer, attr.Expression, this);
                setAttr(analyzer, attr, rvalue, targetType);
            }
            else if (target is ArrayRef sub)
            {
                DataType valueType = TransformExp(analyzer, sub.array, this);
                var xform = new TypeTransformer(this, analyzer);
                TransformExprs(analyzer, sub.subs, this);
                if (valueType is ListType t)
                {
                    t.setElementType(UnionType.Union(t.eltType, rvalue));
                }
            }
            else if (target != null)
            {
                analyzer.AddProblem(target, "invalid location for assignment");
            }
        }


        /// <summary>
        /// Without specifying a kind, bind determines the kind according to the type
        /// of the scope.
        /// </summary>
        public void BindByScope(Analyzer analyzer, Exp target, DataType rvalue)
        {
            BindingKind kind;
            if (this.stateType == State.StateType.FUNCTION)
            {
                kind = BindingKind.VARIABLE;
            }
            else if (this.stateType == State.StateType.CLASS ||
                  this.stateType == State.StateType.INSTANCE)
            {
                kind = BindingKind.ATTRIBUTE;
            }
            else
            {
                kind = BindingKind.SCOPE;
            }
            this.Bind(analyzer, target, rvalue, kind);
        }

        public void Bind(Analyzer analyzer, Identifier id, DataType rvalue, BindingKind kind)
        {
            if (this.IsGlobalName(id.Name))
            {
                ISet<Binding> bs = this.Lookup(id.Name);
                if (bs != null)
                {
                    foreach (Binding b in bs)
                    {
                        b.AddType(rvalue);
                        analyzer.putRef(id, b);
                    }
                }
            }
            else
            {
                this.Insert(analyzer, id.Name, id, rvalue, kind);
            }
        }

        public void Bind(Analyzer analyzer, List<Exp> xs, DataType rvalue, BindingKind kind)
        {
            if (rvalue is TupleType)
            {
                List<DataType> vs = ((TupleType) rvalue).eltTypes;
                if (xs.Count != vs.Count)
                {
                    ReportUnpackMismatch(analyzer, xs, vs.Count);
                }
                else
                {
                    for (int i = 0; i < xs.Count; i++)
                    {
                        this.Bind(analyzer, xs[i], vs[i], kind);
                    }
                }
            }
            else
            {
                if (rvalue is ListType)
                {
                    Bind(analyzer, xs, ((ListType) rvalue).toTupleType(xs.Count), kind);
                }
                else if (rvalue is DictType)
                {
                    Bind(analyzer, xs, ((DictType) rvalue).ToTupleType(xs.Count), kind);
                }
                else if (rvalue.isUnknownType())
                {
                    foreach (Exp x in xs)
                    {
                        this.Bind(analyzer, x, DataType.Unknown, kind);
                    }
                }
                else if (xs.Count > 0)
                {
                    analyzer.AddProblem(xs[0].Filename,
                            xs[0].Start,
                            xs[xs.Count - 1].End,
                            "unpacking non-iterable: " + rvalue);
                }
            }
        }

        private static void ReportUnpackMismatch(Analyzer analyzer, List<Exp> xs, int vsize)
        {
            int xsize = xs.Count;
            int beg = xs[0].Start;
            int end = xs[xs.Count - 1].End;
            int diff = xsize - vsize;
            string msg;
            if (diff > 0)
            {
                msg = "ValueError: need more than " + vsize + " values to unpack";
            }
            else
            {
                msg = "ValueError: too many values to unpack";
            }
            analyzer.AddProblem(xs[0].Filename, beg, end, msg);
        }

        // iterator
        public void BindIterator(Analyzer analyzer, Exp target, Exp iter, DataType iterType, BindingKind kind)
        {
            if (iterType is ListType)
            {
                this.Bind(analyzer, target, ((ListType) iterType).eltType, kind);
            }
            else if (iterType is TupleType)
            {
                this.Bind(analyzer, target, ((TupleType) iterType).ToListType().eltType, kind);
            }
            else
            {
                ISet<Binding> ents = iterType.Table.LookupAttribute("__iter__");
                if (ents != null)
                {
                    foreach (Binding ent in ents)
                    {
                        if (ent == null || !(ent.type is FunType))
                        {
                            if (!iterType.isUnknownType())
                            {
                                analyzer.AddProblem(iter, "not an iterable type: " + iterType);
                            }
                            this.Bind(analyzer, target, DataType.Unknown, kind);
                        }
                        else
                        {
                            this.Bind(analyzer, target, ((FunType) ent.type).GetReturnType(), kind);
                        }
                    }
                }
                else
                {
                    this.Bind(analyzer, target, DataType.Unknown, kind);
                }
            }
        }

        private static void setAttr(Analyzer analyzer, AttributeAccess attr, DataType attrType, DataType targetType)
        {
            if (targetType is UnionType)
            {
                ISet<DataType> types = ((UnionType) targetType).types;
                foreach (DataType tp in types)
                {
                    setAttrType(analyzer, attr, tp, attrType);
                }
            }
            else
            {
                setAttrType(analyzer, attr, targetType, attrType);
            }
        }

        private static void setAttrType(Analyzer analyzer, AttributeAccess attr, DataType targetType, DataType attrType)
        {
            if (targetType.isUnknownType())
            {
                analyzer.AddProblem(attr, "Can't set attribute for UnknownType");
                return;
            }
            ISet<Binding> bs = targetType.Table.LookupAttribute(attr.FieldName.Name);
            if (bs != null)
            {
                analyzer.addRef(attr, targetType, bs);
            }
            targetType.Table.Insert(analyzer, attr.FieldName.Name, attr, attrType, BindingKind.ATTRIBUTE);
        }

        public static void TransformExprs(Analyzer analyzer, List<Slice> exprs, State s)
        {
            var x = new TypeTransformer(s, analyzer);
            foreach (var e in exprs)
            {
                e.Accept(x);
            }
        }

        public static DataType TransformExp(Analyzer analyzer, Exp n, State s)
        {
            return n.Accept(new TypeTransformer(s, analyzer));
        }
    }
}