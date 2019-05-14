using System;
using System.Collections.Generic;
using System.Linq;
using Pytocs.Core.Types;
using Pytocs.Core.Syntax;

namespace Pytocs.Core.TypeInference
{
    public class TypeTransformer : 
        IStatementVisitor<DataType>,
        IExpVisitor<DataType>
    {
        private State scope;
        private Analyzer analyzer;

        public TypeTransformer(State s, Analyzer analyzer)
        {
            this.scope = s;
            this.analyzer = analyzer;
        }

        private DataType Register(Exp e, DataType t)
        {
            analyzer.AddExpType(e, t);
            return t;
        }

        public DataType VisitAliasedExp(AliasedExp a)
        {
            return DataType.Unknown;
        }

        public DataType VisitAsync(AsyncStatement a)
        {
            var dt = VisitFunctionDef((FunctionDef)a.Statement, true);
            return dt;
        }

        public DataType VisitAssert(AssertStatement a)
        {
            if (a.Tests != null)
            {
                foreach (var t in a.Tests)
                    t.Accept(this);
            }
            if (a.Message != null)
            {
                a.Message.Accept(this);
            }
            return DataType.Cont;
        }

        public DataType VisitAssignExp(AssignExp a)
        {
            if (scope.stateType == State.StateType.CLASS &&
                a.Dst is Identifier id &&
                id.Name == "__slots__")
            {
                // The __slots__ attribute needs to be handled specially:
                // it actually introduces new attributes.
                BindClassSlots(a.Src);
            }
            else
            {
                DataType valueType = a.Src.Accept(this);
                scope.BindByScope(analyzer, a.Dst, valueType);
            }
            return DataType.Cont;
        }

        private void BindClassSlots(Exp eSlotNames)
        {
            IEnumerable<Exp> slotNames = null;
            switch (eSlotNames)
            {
            case PyList srcList:
                slotNames = srcList.elts;
                break;
            case PyTuple srcTuple:
                slotNames = srcTuple.values;
                break;
            case ExpList expList:
                slotNames = expList.Expressions;
                break;
            }
            if (slotNames == null)
            {
                //$TODO: dynamically generated slots are hard.
            }
            else
            {
                // Generate an attribute binding for each slot.
                foreach (var slotName in slotNames.OfType<Str>())
                {
                    var id = new Identifier(slotName.s, slotName.Filename, slotName.Start, slotName.End);
                    scope.Bind(analyzer, id, DataType.Unknown, BindingKind.ATTRIBUTE);
                }
            }
        }

        public DataType VisitAwait(AwaitExp e)
        {
            var dt = e.exp.Accept(this);
            return dt;
        }

        public DataType VisitFieldAccess(AttributeAccess a)
        {
            // the form of ::A in ruby
            if (a.Expression == null)
            {
                return a.FieldName.Accept(this);
            }

            var targetType = a.Expression.Accept(this);
            if (targetType is UnionType ut)
            {
                ISet<DataType> types = ut.types;
                DataType retType = DataType.Unknown;
                foreach (DataType tt in types)
                {
                    retType = UnionType.Union(retType, GetAttrType(a, tt));
                }
                return retType;
            }
            else
            {
                return GetAttrType(a, targetType);
            }
        }

        public DataType GetAttrType(AttributeAccess a, DataType targetType)
        {
            ISet<Binding> bs = targetType.Table.LookupAttribute(a.FieldName.Name);
            if (bs == null)
            {
                analyzer.AddProblem(a.FieldName, "attribute not found in type: " + targetType);
                DataType t = DataType.Unknown;
                t.Table.Path = targetType.Table.ExtendPath(analyzer, a.FieldName.Name);
                return t;
            }
            else
            {
                analyzer.addRef(a, targetType, bs);
                return State.MakeUnion(bs);
            }
        }

        public DataType VisitBinExp(BinExp b)
        {
            DataType ltype = b.l.Accept(this);
            DataType rtype = b.r.Accept(this);
            if (b.op.IsBoolean())
            {
                return DataType.Bool;
            }
            else
            {
                return UnionType.Union(ltype, rtype);
            }
        }

        public DataType VisitSuite(SuiteStatement b)
        {
            // first pass: mark global names
            var globalNames = b.stmts
                .OfType<GlobalStatement>()
                .SelectMany(g => g.names)
                .Concat(b.stmts
                    .OfType<NonlocalStatement>()
                    .SelectMany(g => g.names));
            foreach (var id in globalNames)
            {
                scope.AddGlobalName(id.Name);
                ISet<Binding> nb = scope.Lookup(id.Name);
                if (nb != null)
                {
                    analyzer.putRef(id, nb);
                }
            }

            bool returned = false;
            DataType retType = DataType.Unknown;
            foreach (var n in b.stmts)
            {
                DataType t = n.Accept(this);
                if (!returned)
                {
                    retType = UnionType.Union(retType, t);
                     if (!UnionType.Contains(t, DataType.Cont))
                    {
                        returned = true;
                        retType = UnionType.Remove(retType, DataType.Cont);
                    }
                }
            }
            return retType;
        }

        public DataType VisitBreak(BreakStatement b)
        {
            return DataType.None;
        }

        public DataType VisitBooleanLiteral(BooleanLiteral b)
        {
            return DataType.Bool;
        }

        /// <remarks>
        /// Most of the work here is done by the static method invoke, which is also
        /// used by Analyzer.applyUncalled. By using a static method we avoid building
        /// a NCall node for those dummy calls.
        /// </remarks>
        public DataType VisitApplication(Application c)
        {
            var fun = c.fn.Accept(this);
            var dtPos = c.args.Select(a => a.defval.Accept(this)).ToList();
            var hash = new Dictionary<string, DataType>();
            if (c.keywords != null)
            {
                foreach (var k in c.keywords)
                {
                    hash[k.name.Name] = k.defval.Accept(this);
                }
            }
            var dtKw = c.kwargs?.Accept(this);
            var dtStar = c.stargs?.Accept(this);
            if (fun is UnionType un)
            {
                ISet<DataType> types = un.types;
                DataType retType = DataType.Unknown;
                foreach (DataType ft in types)
                {
                    DataType t = ResolveCall(c, ft, dtPos, hash, dtKw, dtStar);
                    retType = UnionType.Union(retType, t);
                }
                return retType;
            }
            else
            {
                return ResolveCall(c, fun, dtPos, hash, dtKw, dtStar);
            }
        }

        private DataType ResolveCall(
            Application c,
            DataType fun,
            List<DataType> pos,
            IDictionary<string, DataType> hash,
            DataType kw,
            DataType star)
        {
            if (fun is FunType ft)
            {
                return Apply(analyzer, ft, pos, hash, kw, star, c);
            }
            else if (fun is ClassType)
            {
                var instance = new InstanceType(fun);
                ApplyConstructor(analyzer, instance, c, pos);
                return instance;
            }
            else
            {
                AddWarning(c, "calling non-function and non-class: " + fun);
                return DataType.Unknown;
            }
        }

        public static void ApplyConstructor(Analyzer analyzer, InstanceType i, Application call, List<DataType> args)
        {
            if (i.Table.LookupAttributeType("__init__") is FunType initFunc && initFunc.Definition != null)
            {
                initFunc.SelfType = i;
                Apply(analyzer, initFunc, args, null, null, null, call);
                initFunc.SelfType = null;
            }
        }


        /// <summary>
        /// Called when an application of a function is encountered.
        /// </summary>
        //$TODO: move to Analyzer.
        public static DataType Apply(
            Analyzer analyzer,
            FunType func,
            List<DataType> pos,
            IDictionary<string, DataType> hash,
            DataType kw,
            DataType star,
            Exp call)
        {
            analyzer.RemoveUncalled(func);

            if (func.Definition != null && !func.Definition.called)
            {
                analyzer.CalledFunctions++;
                func.Definition.called = true;
            }

            if (func.Definition == null)
            {
                // func without definition (possibly builtins)
                return func.GetReturnType();
            }
            else if (call != null && analyzer.InStack(call))
            {
                // Recursive call, ignore.
                func.SelfType = null;
                return DataType.Unknown;
            }

            if (call != null)
            {
                analyzer.pushStack(call);
            }

            var pTypes = new List<DataType>();

            // Python: bind first parameter to self type
            if (func.SelfType != null)
            {
                pTypes.Add(func.SelfType);
            }
            else if (func.Class != null)
            {
                pTypes.Add(func.Class.GetInstance());
            }

            if (pos != null)
            {
                pTypes.AddRange(pos);
            }

            BindMethodAttrs(analyzer, func);

            State funcTable = new State(func.env, State.StateType.FUNCTION);
            if (func.Table.Parent != null)
            {
                funcTable.Path = func.Table.Parent.ExtendPath(analyzer, func.Definition.name.Name);
            }
            else
            {
                funcTable.Path = func.Definition.name.Name;
            }

            DataType fromType = BindParameters(analyzer,
                call, func.Definition, funcTable, func.Definition.parameters,
                func.Definition.vararg, func.Definition.kwarg,
                pTypes, func.defaultTypes, hash, kw, star);

            if (func.arrows.TryGetValue(fromType, out var cachedTo))
            {
                func.SelfType = null;
                return cachedTo;
            }
            else
            {
                DataType toType = func.Definition.body.Accept(new TypeTransformer(funcTable, analyzer));
                if (MissingReturn(toType))
                {
                    analyzer.AddProblem(func.Definition.name, "Function doesn't always return a value");
                    if (call != null)
                    {
                        analyzer.AddProblem(call, "Call doesn't always return a value");
                    }
                }

                toType = UnionType.Remove(toType, DataType.Cont);
                func.AddMapping(fromType, toType);
                func.SelfType = null;
                return toType;
            }
        }

        public static DataType FirstArgumentType(FunctionDef f, FunType func, DataType selfType)
        {
            if (f.parameters.Count == 0)
                return null;
            if (!func.Definition.IsStaticMethod())
            {
                if (func.Definition.IsClassMethod())
                {
                    if (func.Class != null)
                    {
                        return func.Class;
                    }
                    else if (selfType != null && selfType is InstanceType inst) {
                        return inst.classType;
                    }
                }
                else
                {
                    // usual method
                    if (selfType != null)
                    {
                        return selfType;
                    }
                    else
                    {
                        if (func.Class != null)
                        {
                            if (func.Definition.name.Name != "__init__")
                            {
                                throw new NotImplementedException("return func.Class.getInstance(null, this, call));");
                            }
                            else
                            {
                                return func.Class.GetInstance();
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Binds the parameters of a call to the called function.
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="call"></param>
        /// <param name="func"></param>
        /// <param name="funcTable"></param>
        /// <param name="parameters"></param>
        /// <param name="rest"></param>
        /// <param name="restKw"></param>
        /// <param name="pTypes"></param>
        /// <param name="dTypes"></param>
        /// <param name="hash"></param>
        /// <param name="kw"></param>
        /// <param name="star"></param>
        /// <returns></returns>
        private static DataType BindParameters(
            Analyzer analyzer,
            Node call,
            FunctionDef func,
            State funcTable,
            List<Parameter> parameters,
            Identifier rest,
            Identifier restKw,
            List<DataType> pTypes,
            List<DataType> dTypes,
            IDictionary<string, DataType> hash,
            DataType kw,
            DataType star)
        {
            TupleType fromType = analyzer.TypeFactory.CreateTuple();
            int pSize = parameters == null ? 0 : parameters.Count;
            int aSize = pTypes == null ? 0 : pTypes.Count;
            int dSize = dTypes == null ? 0 : dTypes.Count;
            int nPos = pSize - dSize;

            if (star != null && star is ListType list)
            {
                star = list.ToTupleType();
            }

            for (int i = 0, j = 0; i < pSize; i++)
            {
                Parameter param = parameters[i];
                DataType aType;
                if (i < aSize)
                {
                    aType = pTypes[i];
                }
                else if (i - nPos >= 0 && i - nPos < dSize)
                {
                    aType = dTypes[i - nPos];
                }
                else
                {
                    if (hash != null && hash.ContainsKey(parameters[i].Id.Name))
                    {
                        aType = hash[parameters[i].Id.Name];
                        hash.Remove(parameters[i].Id.Name);
                    }
                    else
                    {
                        if (star != null && star is TupleType tup &&
                                j < tup.eltTypes.Count)
                        {
                            aType = tup.get(j);
                            ++j;
                        }
                        else
                        {
                            aType = DataType.Unknown;
                            if (call != null)
                            {
                                analyzer.AddProblem(parameters[i].Id, //$REVIEW: should be using identifiers
                                        "unable to bind argument:" + parameters[i]);
                            }
                        }
                    }
                }
                funcTable.Bind(analyzer, param.Id, aType, BindingKind.PARAMETER);
                fromType.add(aType);
            }

            if (restKw != null)
            {
                DataType dt;
                if (hash != null && hash.Count > 0)
                {
                    DataType hashType = UnionType.CreateUnion(hash.Values);
                    dt = analyzer.TypeFactory.CreateDict(DataType.Str, hashType);
                }
                else
                {
                    dt = DataType.Unknown;
                }
                funcTable.Bind(
                        analyzer,
                        restKw,
                        dt,
                        BindingKind.PARAMETER);
            }

            if (rest != null)
            {
                if (pTypes.Count > pSize)
                {
                    DataType restType = analyzer.TypeFactory.CreateTuple(pTypes.SubList(pSize, pTypes.Count).ToArray());
                    funcTable.Bind(analyzer, rest, restType, BindingKind.PARAMETER);
                }
                else
                {
                    funcTable.Bind(
                            analyzer,
                            rest,
                            DataType.Unknown,
                            BindingKind.PARAMETER);
                }
            }
            return fromType;
        }

        static bool MissingReturn(DataType toType)
        {
            bool hasNone = false;
            bool hasOther = false;

            if (toType is UnionType ut)
            {
                foreach (DataType t in ut.types)
                {
                    if (t == DataType.None || t == DataType.Cont)
                    {
                        hasNone = true;
                    }
                    else
                    {
                        hasOther = true;
                    }
                }
            }
            return hasNone && hasOther;
        }

        static void BindMethodAttrs(Analyzer analyzer, FunType cl)
        {
            if (cl.Table.Parent != null)
            {
                DataType cls = cl.Table.Parent.DataType;
                if (cls != null && cls is ClassType)
                {
                    AddReadOnlyAttr(analyzer, cl, "im_class", cls, BindingKind.CLASS);
                    AddReadOnlyAttr(analyzer, cl, "__class__", cls, BindingKind.CLASS);
                    AddReadOnlyAttr(analyzer, cl, "im_self", cls, BindingKind.ATTRIBUTE);
                    AddReadOnlyAttr(analyzer, cl, "__self__", cls, BindingKind.ATTRIBUTE);
                }
            }
        }

        static void AddReadOnlyAttr(
            Analyzer analyzer,
            FunType fun,
            string name,
            DataType type,
            BindingKind kind)
        {
            Node loc = Builtins.newDataModelUrl("the-standard-type-hierarchy");
            Binding b = analyzer.CreateBinding(name, loc, type, kind);
            fun.Table.Update(name, b);
            b.IsSynthetic = true;
            b.IsStatic = true;
        }

        public void AddSpecialAttribute(State s, string name, DataType proptype)
        {
            Binding b = analyzer.CreateBinding(name, Builtins.newTutUrl("classes.html"), proptype, BindingKind.ATTRIBUTE);
            s.Update(name, b);
            b.IsSynthetic = true;
            b.IsStatic = true;
        }

        public DataType VisitClass(ClassDef c)
        {
            var path = scope.ExtendPath(analyzer, c.name.Name);
            var classType = new ClassType(c.name.Name, scope, path);
            var baseTypes = new List<DataType>();
            foreach (var @base in c.args)
            {
                DataType baseType = @base.Accept(this);
                switch (baseType)
                {
                case ClassType _:
                    classType.AddSuper(baseType);
                    break;
                case UnionType ut:
                    foreach (DataType parent in ut.types)
                    {
                        classType.AddSuper(parent);
                    }
                    break;
                default:
                    analyzer.AddProblem(@base, @base + " is not a class");
                    break;
                }
                baseTypes.Add(baseType);
            }

            // XXX: Not sure if we should add "bases", "name" and "dict" here. They
            // must be added _somewhere_ but I'm just not sure if it should be HERE.
            AddSpecialAttribute(classType.Table, "__bases__", analyzer.TypeFactory.CreateTuple(baseTypes.ToArray()));
            AddSpecialAttribute(classType.Table, "__name__", DataType.Str);
            AddSpecialAttribute(classType.Table, "__dict__",
                    analyzer.TypeFactory.CreateDict(DataType.Str, DataType.Unknown));
            AddSpecialAttribute(classType.Table, "__module__", DataType.Str);
            AddSpecialAttribute(classType.Table, "__doc__", DataType.Str);

            // Bind ClassType to name here before resolving the body because the
            // methods need this type as self.
            scope.Bind(analyzer, c.name, classType, BindingKind.CLASS);
            if (c.body != null)
            {
                var sOld = this.scope;
                this.scope = classType.Table;
                c.body.Accept(this);
                this.scope = sOld;
            }
            return DataType.Cont;
        }

        public DataType VisitComment(CommentStatement c)
        {
            return DataType.Cont;
        }

        public DataType VisitImaginary(ImaginaryLiteral c)
        {
            return DataType.Complex;
        }

        public DataType VisitCompFor(CompFor f)
        {
            var it = f.variable.Accept(this);
            scope.BindIterator(analyzer, f.variable, f.collection, it, BindingKind.SCOPE);
            //f.visit(node.ifs, s);
            return f.variable.Accept(this);
        }

        public DataType VisitCompIf(CompIf i)
        {
            throw new NotImplementedException();
        }

        //public DataType VisitComprehension(Comprehension c)
        //{
        //    Binder.bindIter(fs, s, c.target, c.iter, Binding.Kind.SCOPE);
        //    resolveList(c.ifs);
        //    return c.target.Accept(this);
        //}

        public DataType VisitContinue(ContinueStatement c)
        {
            return DataType.Cont;
        }

        public DataType VisitDel(DelStatement d)
        {
            foreach (var n in d.Expressions.AsList())
            {
                n.Accept(this);
                if (n is Identifier id)
                {
                    scope.Remove(id.Name);
                }
            }
            return DataType.Cont;
        }

        public DataType VisitDictInitializer(DictInitializer d)
        {
            DataType keyType = ResolveUnion(d.KeyValues.Select(kv => kv.Key));
            DataType valType = ResolveUnion(d.KeyValues.Select(kv => kv.Value));
            return analyzer.TypeFactory.CreateDict(keyType, valType);
        }

        /// 
        /// Python's list comprehension will bind the variables used in generators.
        /// This will erase the original values of the variables even after the
        /// comprehension.
        /// 
        public DataType VisitDictComprehension(DictComprehension d)
        {
           // ResolveList(d.generator);
            DataType keyType = d.key.Accept(this);
            DataType valueType = d.value.Accept(this);
            return analyzer.TypeFactory.CreateDict(keyType, valueType);
        }

        public DataType VisitDottedName(DottedName name)
        {
            throw new NotImplementedException();
        }

        public DataType VisitEllipsis(Ellipsis e)
        {
            return DataType.None;
        }

        public DataType VisitExec(ExecStatement e)
        {
            if (e.code != null)
            {
                e.code.Accept(this);
            }
            if (e.globals != null)
            {
                e.globals.Accept(this);
            }
            if (e.locals != null)
            {
                e.locals.Accept(this);
            }
            return DataType.Cont;
        }

        public DataType VisitExp(ExpStatement e)
        {
            if (e.Expression != null)
            {
                e.Expression.Accept(this);
            }
            return DataType.Cont;
        }

        public DataType VisitExpList(ExpList list)
        {
            var t = new TupleType();
            var elTypes = new List<DataType>();
            foreach (var el in list.Expressions)
            {
                var elt = el.Accept(this);
                elTypes.Add(elt);
            }
            return new TupleType(elTypes);
        }

        public DataType VisitExtSlice(List<Slice> e)
        {
            foreach (var d in e)
            {
                d.Accept(this);
            }
            return new ListType();
        }

        public DataType VisitFor(ForStatement f)
        {
            scope.BindIterator(analyzer, f.exprs, f.tests, f.tests.Accept(this), BindingKind.SCOPE);
            DataType ret;
            if (f.Body == null)
            {
                ret = DataType.Unknown;
            }
            else
            {
                ret = f.Body.Accept(this);
            }
            if (f.Else != null)
            {
                ret = UnionType.Union(ret, f.Else.Accept(this));
            }
            return ret;
        }

        public DataType VisitLambda(Lambda lambda)
        {
            State env = scope.getForwarding();
            var fun = new FunType(lambda, env);
            fun.Table.Parent = this.scope;
            fun.Table.Path = scope.ExtendPath(analyzer, "{lambda}");
            fun.SetDefaultTypes(ResolveList(lambda.args.Select(p => p.test)));
            analyzer.AddUncalled(fun);
            return fun;
        }

        public DataType VisitFunctionDef(FunctionDef f)
        {
            return VisitFunctionDef(f, false);
        }

        public DataType VisitFunctionDef(FunctionDef f, bool isAsync)
        {
            State env = scope.getForwarding();
            FunType fun = new FunType(f, env);
            fun.Table.Parent = this.scope;
            fun.Table.Path = scope.ExtendPath(analyzer, f.name.Name);
            fun.SetDefaultTypes(ResolveList(f.parameters
                .Where(p => p.test != null)
                .Select(p => p.test)));
            if (isAsync)
            {
                fun = fun.MakeAwaitable();
            }
            analyzer.AddUncalled(fun);

            BindingKind funkind;
            if (scope.stateType == State.StateType.CLASS)
            {
                if ("__init__" == f.name.Name)
                {
                    funkind = BindingKind.CONSTRUCTOR;
                }
                else
                {
                    funkind = BindingKind.METHOD;
                }
            }
            else
            {
                funkind = BindingKind.FUNCTION;
            }

            var ct = scope.DataType as ClassType;
            if (ct != null)
            {
                fun.Class = ct;
            }

            scope.Bind(analyzer, f.name, fun, funkind);
            var firstArgType = FirstArgumentType(f, fun, ct);
            if (firstArgType != null)
            {
                fun.Table.Bind(analyzer, f.parameters[0].Id, firstArgType, BindingKind.PARAMETER);
            }

            var sOld = this.scope;
            this.scope = fun.Table;
            f.body.Accept(this);
            this.scope = sOld;
            
            return DataType.Cont;
        }

        public DataType VisitGlobal(GlobalStatement g)
        {
            // Do nothing here because global names are processed by VisitSuite
            return DataType.Cont;
        }

        public DataType VisitNonLocal(NonlocalStatement non)
        {
            // Do nothing here because global names are processed by VisitSuite
            return DataType.Cont;
        }

        public DataType VisitHandler(ExceptHandler h)
        {
            DataType typeval = DataType.Unknown;
            if (h.type != null)
            {
                typeval = h.type.Accept(this);
            }
            if (h.name != null)
            {
                scope.BindByScope(analyzer, h.name, typeval);
            }
            if (h.body != null)
            {
                return h.body.Accept(this);
            }
            else
            {
                return DataType.Unknown;
            }
        }

        public DataType VisitIf(IfStatement i)
        {
            DataType type1, type2;
            State s1 = scope.Clone();
            State s2 = scope.Clone();

            // Ignore condition for now
            i.Test.Accept(this);

            if (i.Then != null)
            {
                type1 = i.Then.Accept(this);
            }
            else
            {
                type1 = DataType.Cont;
            }

            if (i.Else != null)
            {
                type2 = i.Else.Accept(this);
            }
            else
            {
                type2 = DataType.Cont;
            }

            bool cont1 = UnionType.Contains(type1, DataType.Cont);
            bool cont2 = UnionType.Contains(type2, DataType.Cont);

            // Decide which branch affects the downstream state
            if (cont1 && cont2)
            {
                s1.Merge(s2);
                scope.Overwrite(s1);
            }
            else if (cont1)
            {
                scope.Overwrite(s1);
            }
            else if (cont2)
            {
                scope.Overwrite(s2);
            }
            return UnionType.Union(type1, type2);
        }

        public DataType VisitTest(TestExp i)
        {
            DataType type1, type2;
            i.Condition.Accept(this);

            if (i.Consequent != null)
            {
                type1 = i.Consequent.Accept(this);
            }
            else
            {
                type1 = DataType.Cont;
            }
            if (i.Alternative != null)
            {
                type2 = i.Alternative.Accept(this);
            }
            else
            {
                type2 = DataType.Cont;
            }
            return UnionType.Union(type1, type2);
        }

        public DataType VisitImport(ImportStatement i)
        {
            foreach (var a in i.names)
            {
                DataType mod = analyzer.LoadModule(a.orig.segs, scope);
                if (mod == null)
                {
                    analyzer.AddProblem(i, $"Cannot load module {a}");
                }
                else if (a.alias != null)
                {
                    scope.Insert(analyzer, a.alias.Name, a.alias, mod, BindingKind.VARIABLE);
                }
            }
            return DataType.Cont;
        }

        public DataType VisitFrom(FromStatement i)
        {
            if (i.DottedName == null)
            {
                return DataType.Cont;
            }

            DataType dtModule = analyzer.LoadModule(i.DottedName.segs, scope);
            if (dtModule == null)
            {
                analyzer.AddProblem(i, "Cannot load module");
            }
            else if (i.isImportStar())
            {
                ImportStar(i, dtModule);
            }
            else
            {
                foreach (var a in i.AliasedNames)
                {
                    Identifier first = a.orig.segs[0];
                    ISet<Binding> bs = dtModule.Table.Lookup(first.Name);
                    if (bs != null)
                    {
                        if (a.alias != null)
                        {
                            scope.Update(a.alias.Name, bs);
                            analyzer.putRef(a.alias, bs);
                        }
                        else
                        {
                            scope.Update(first.Name, bs);
                            analyzer.putRef(first, bs);
                        }
                    }
                    else
                    {
                        List<Identifier> ext = new List<Identifier>(i.DottedName.segs)
                        {
                            first
                        };
                        DataType mod2 = analyzer.LoadModule(ext, scope);
                        if (mod2 != null)
                        {
                            if (a.alias != null)
                            {
                                scope.Insert(analyzer, a.alias.Name, a.alias, mod2, BindingKind.VARIABLE);
                            }
                            else
                            {
                                scope.Insert(analyzer, first.Name, first, mod2, BindingKind.VARIABLE);
                            }
                        }
                    }
                }
            }
            return DataType.Cont;
        }

        private void ImportStar(FromStatement i, DataType mt)
        {
            if (mt == null || mt.file == null)
            {
                return;
            }

            Module node = analyzer.GetAstForFile(mt.file);
            if (node == null)
            {
                return;
            }

            DataType allType = mt.Table.LookupType("__all__");

            List<string> names = new List<string>();
            if (allType != null && allType is ListType lt)
            {
                foreach (var s in lt.values.OfType<string>()) 
                {
                    names.Add(s);
                }
            }

            if (names.Count > 0)
            {
                int start = i.Start;
                foreach (string name in names)
                {
                    ISet<Binding> b = mt.Table.LookupLocal(name);
                    if (b != null)
                    {
                        scope.Update(name, b);
                    }
                    else
                    {
                        var m2 = new List<Identifier>(i.DottedName.segs);
                        var fakeName = new Identifier(name, i.Filename, start, start + name.Length);
                        m2.Add(fakeName);
                        DataType type = analyzer.LoadModule(m2, scope);
                        if (type != null)
                        {
                            start += name.Length;
                            scope.Insert(analyzer, name, fakeName, type, BindingKind.VARIABLE);
                        }
                    }
                }
            }
            else
            {
                // Fall back to importing all names not starting with "_".
                foreach (var e in mt.Table.entrySet().Where(en => !en.Key.StartsWith("_")))
                {
                    scope.Update(e.Key, e.Value);
                }
            }
        }

        public DataType VisitArgument(Argument arg)
        {
            return arg.defval.Accept(this);
        }

        public DataType VisitBigLiteral(BigLiteral i)
        {

            return Register(i, DataType.Int);
        }

        public DataType VisitIntLiteral(IntLiteral i)
        {
            return DataType.Int;
        }

        public DataType VisitList(PyList l)
        {
            var listType = new ListType();
            if (l.elts.Count == 0)
                return Register(l, listType);  // list of unknown.
            foreach (var exp in l.elts)
            {
                listType.Add(exp.Accept(this));
                if (exp is Str sExp)
                {
                    listType.addValue(sExp.s);
                }
            }
            return Register(l, listType);
        }

        /// <remarks>
        /// Python's list comprehension will bind the variables used in generators.
        /// This will erase the original values of the variables even after the
        /// comprehension.
        /// </remarks>
        public DataType VisitListComprehension(ListComprehension l)
        {
            l.Collection.Accept(this);
            return analyzer.TypeFactory.CreateList(l.Projection.Accept(this));
        }

        /// 
        /// Python's list comprehension will erase any variable used in generators.
        /// This is wrong, but we "respect" this bug here.
        /// 
        public DataType VisitGeneratorExp(GeneratorExp g)
        {
            g.Collection.Accept(this);
            return analyzer.TypeFactory.CreateList(g.Projection.Accept(this));
        }

        public DataType VisitLongLiteral(LongLiteral l)
        {
            return DataType.Int;
        }

        public ModuleType VisitModule(Module m)
        {
            string qname = null;
            if (m.Filename != null)
            {
                // This will return null iff specified file is not prefixed by
                // any path in the module search path -- i.e., the caller asked
                // the analyzer to load a file not in the search path.
                qname = analyzer.GetModuleQname(m.Filename);
            }
            if (qname == null)
            {
                qname = m.Name;
            }

            var mt = analyzer.TypeFactory.CreateModule(m.Name, m.Filename, qname, analyzer.GlobalTable);

            scope.Insert(analyzer, analyzer.GetModuleQname(m.Filename), m, mt, BindingKind.MODULE);
            if (m.body != null)
            {
                var sOld = this.scope;
                this.scope = mt.Table;
                m.body.Accept(this);
                this.scope = sOld;
            }
            return mt;
        }

        public DataType VisitIdentifier(Identifier id)
        {
            ISet<Binding> b = scope.Lookup(id.Name);
            if (b != null)
            {
                analyzer.putRef(id, b);
                analyzer.Resolved.Add(id);
                analyzer.Unresolved.Remove(id);
                return State.MakeUnion(b);
            }
            else if (id.Name == "True" || id.Name == "False")
            {
                return DataType.Bool;
            }
            else
            {
                analyzer.AddProblem(id, "unbound variable " + id.Name);
                analyzer.Unresolved.Add(id);
                DataType t = DataType.Unknown;
                t.Table.Path = scope.ExtendPath(analyzer, id.Name);
                return t;
            }
        }

        public DataType VisitNoneExp()
        {
            return DataType.None;
        }

        public DataType VisitPass(PassStatement p)
        {
            return DataType.Cont;
        }

        public DataType VisitPrint(PrintStatement p)
        {
            if (p.outputStream != null)
            {
                p.outputStream.Accept(this);
            }
            if (p.args != null)
            {
                ResolveList(p.args.Select(a => a.defval));
            }
            return DataType.Cont;
        }

        public DataType VisitRaise(RaiseStatement r)
        {
            if (r.exToRaise != null)
            {
                r.exToRaise.Accept(this);
            }
            if (r.exOriginal != null)
            {
                r.exOriginal.Accept(this);
            }
            if (r.traceback != null)
            {
                r.traceback.Accept(this);
            }
            return DataType.Cont;
        }

        public DataType VisitRealLiteral(RealLiteral r)
        {
            return DataType.Float;
        }

        //public DataType VisitRepr(Repr r)
        //{
        //    if (r.value != null)
        //    {
        //        r.value.Accept(this);
        //    }
        //    return DataType.STR;
        //}

        public DataType VisitReturn(ReturnStatement r)
        {
            if (r.Expression == null)
            {
                return DataType.None;
            }
            else
            {
                return r.Expression.Accept(this);
            }
        }

        public DataType VisitSet(PySet s)
        {
            DataType valType = ResolveUnion(s.exps);
            return new SetType(valType);
        }

        public DataType VisitSetComprehension(SetComprehension s)
        {
            s.Collection.Accept(this);
            return new SetType(s.Projection.Accept(this));
        }

        public DataType VisitSlice(Slice s)
        {
            if (s.lower != null)
            {
                s.lower.Accept(this);
            }
            if (s.step != null)
            {
                s.step.Accept(this);
            }
            if (s.upper != null)
            {
                s.upper.Accept(this);
            }
            return analyzer.TypeFactory.CreateList();
        }

        public DataType VisitStarExp(StarExp s)
        {
            return s.e.Accept(this);
        }

        public DataType VisitStr(Str s)
        {
            return DataType.Str;
        }

        public DataType VisitBytes(Bytes b)
        {
            return DataType.Str;
        }

        public DataType VisitArrayRef(ArrayRef s)
        {
            DataType vt = s.array.Accept(this);
            DataType st;
            if (s.subs == null || s.subs.Count == 0)
                st = null;
            else if (s.subs.Count == 1)
                st = s.subs[0].Accept(this);
            else
                st = VisitExtSlice(s.subs);

            if (vt is UnionType)
            {
                DataType retType = DataType.Unknown;
                foreach (DataType t in ((UnionType)vt).types)
                {
                    retType = UnionType.Union( retType, GetSubscript(s, t, st));
                }
                return retType;
            }
            else
            {
                return GetSubscript(s, vt, st);
            }
        }

        public DataType GetSubscript(ArrayRef s, DataType vt, DataType st)
        {
            if (vt.isUnknownType())
            {
                return DataType.Unknown;
            }
            switch (vt) {
            case ListType list:
                return GetListSubscript(s, list, st);
            case TupleType tup:
                return GetListSubscript(s, tup.ToListType(), st);
            case DictType dt:
                if (!dt.KeyType.Equals(st))
                {
                    AddWarning(s, "Possible KeyError (wrong type for subscript)");
                }
                return dt.ValueType;
            case StrType _:
                if (st != null && (st is ListType || st.isNumType()))
                {
                    return vt;
                }
                else
                {
                    AddWarning(s, "Possible KeyError (wrong type for subscript)");
                    return DataType.Unknown;
                }
            default:
                return DataType.Unknown;
            }
        }

        private DataType GetListSubscript(ArrayRef s, DataType vt, DataType st)
        {
            if (vt is ListType list)
            {
                if (st != null && st is ListType)
                {
                    return vt;
                }
                else if (st == null || st.isNumType())
                {
                    return list.eltType;
                }
                else
                {
                    DataType sliceFunc = vt.Table.LookupAttributeType("__getslice__");
                    if (sliceFunc == null)
                    {
                        AddError(s, "The type can't be sliced: " + vt);
                        return DataType.Unknown;
                    }
                    else if (sliceFunc is FunType ft)
                    {
                        return Apply(analyzer, ft, null, null, null, null, s);
                    }
                    else
                    {
                        AddError(s, "The type's __getslice__ method is not a function: " + sliceFunc);
                        return DataType.Unknown;
                    }
                }
            }
            else
            {
                return DataType.Unknown;
            }
        }

        public DataType VisitTry(TryStatement t)
        {
            DataType tp1 = DataType.Unknown;
            DataType tp2 = DataType.Unknown;
            DataType tph = DataType.Unknown;
            DataType tpFinal = DataType.Unknown;

            if (t.exHandlers != null)
            {
                foreach (var h in t.exHandlers)
                {
                    tph = UnionType.Union(tph, VisitHandler(h));
                }
            }

            if (t.body != null)
            {
                tp1 = t.body.Accept(this);
            }

            if (t.elseHandler != null)
            {
                tp2 = t.elseHandler.Accept(this);
            }

            if (t.finallyHandler != null)
            {
                tpFinal = t.finallyHandler.Accept(this);
            }
            return new UnionType(tp1, tp2, tph, tpFinal);
        }

        public DataType VisitTuple(PyTuple t)
        {
            TupleType tt = analyzer.TypeFactory.CreateTuple();
            foreach (var e in t.values)
            {
                tt.add(e.Accept(this));
            }
            return tt;
        }

        public DataType VisitUnary(UnaryExp u)
        {
            return u.e.Accept(this);
        }

        public DataType VisitUrl(Url s)
        {
            return DataType.Str;
        }

        public DataType VisitWhile(WhileStatement w)
        {
            w.Test.Accept(this);
            DataType t = DataType.Unknown;

            if (w.Body != null)
            {
                t = w.Body.Accept(this);
            }

            if (w.Else != null)
            {
                t = UnionType.Union(t, w.Else.Accept(this));
            }
            return t;
        }

        public DataType VisitWith(WithStatement w)
        {
            foreach (var item in w.items)
            {
                DataType val = item.t.Accept(this);
                if (item.e != null)
                {
                    scope.BindByScope(analyzer, item.e, val);
                }
            }
            return w.body.Accept(this);
        }

        public DataType VisitYieldExp(YieldExp y)
        {
            if (y.exp != null)
            {
                return analyzer.TypeFactory.CreateList(y.exp.Accept(this));
            }
            else
            {
                return DataType.None;
            }
        }

        public DataType VisitYieldFromExp(YieldFromExp y)
        {
            if (y.Expression != null)
            {
                return analyzer.TypeFactory.CreateList(y.Expression.Accept(this));
            }
            else
            {
                return DataType.None;
            }
        }

        public DataType VisitYield(YieldStatement y)
        {
            if (y.Expression != null)
                return analyzer.TypeFactory.CreateList(y.Expression.Accept(this));
            else
                return DataType.None;
        }

        protected void AddError(Node n, string msg)
        {
            analyzer.AddProblem(n, msg);
        }

        protected void AddWarning(Node n, string msg)
        {
            analyzer.AddProblem(n, msg);
        }

        /// <summary>
        /// Resolves each element, and constructs a result list.
        /// </summary>
        private List<DataType> ResolveList(IEnumerable<Exp> nodes)
        {
            if (nodes == null)
            {
                return null;
            }
            else
            {
                return nodes
                    .Select(n => n?.Accept(this))
                    .ToList();
            }
        }

        /// <summary>
        /// Utility method to resolve every node in <param name="nodes"/> and
        /// return the union of their types.  If <param name="nodes" /> is empty or
        /// null, returns a new {@link Pytocs.Core.Types.UnknownType}.
        /// </summary>
        public DataType ResolveUnion(IEnumerable<Exp> nodes)
        {
            DataType result = DataType.Unknown;
            foreach (var node in nodes)
            {
                DataType nodeType = node.Accept(this);
                result = UnionType.Union(result, nodeType);
            }
            return result;
        }
    }

    public static class ListEx
    {
        public static List<T> SubList<T>(this List<T> list, int iMin, int iMac)
        {
            return list.Skip(iMin).Take(iMac - iMin).ToList();
        }
    }
}