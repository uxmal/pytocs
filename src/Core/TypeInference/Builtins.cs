#region License
//  Copyright 2015-2022 John Källén
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
using Url = Pytocs.Core.Syntax.Url;
using Pytocs.Core.Types;

namespace Pytocs.Core.TypeInference
{
#pragma warning disable IDE1006 // Naming Styles

    //$REVIEW: This file is messy. Should clean up.
    /// <summary>
    /// Metadata for all Python builtin functions.
    /// </summary>
    public class Builtins
    {
        public const string LIBRARY_URL = "http://docs.python.org/library/";
        public const string TUTORIAL_URL = "http://docs.python.org/tutorial/";
        public const string REFERENCE_URL = "http://docs.python.org/reference/";
        public const string DATAMODEL_URL = "http://docs.python.org/reference/datamodel#";

        public static Url newLibUrl(string module, string name)
        {
            return newLibUrl(module + ".html#" + name);
        }

        public static Url newLibUrl(string path)
        {
            if (!path.Contains("#") && !path.EndsWith(".html"))
            {
                path += ".html";
            }
            return new Url(LIBRARY_URL + path);
        }

        public static Url newRefUrl(string path)
        {
            return new Url(REFERENCE_URL + path);
        }

        public static Url newDataModelUrl(string path)
        {
            return new Url(DATAMODEL_URL + path);
        }

        public static Url newTutUrl(string path)
        {
            return new Url(TUTORIAL_URL + path);
        }

        private AnalyzerImpl analyzer;

        // XXX:  need to model "types" module and reconcile with these types
        public ModuleType Builtin;
        public ClassType objectType;
        public ClassType BaseType;
        public ClassType BaseList;
        public InstanceType BaseListInst;
        public ClassType BaseArray;
        public ClassType BaseDict;
        public ClassType BaseIterable;
        public ClassType BaseStr;
        public ClassType BaseTuple;
        public ClassType BaseModule;
        public ClassType BaseFile;
        public InstanceType BaseFileInst;
        public ClassType BaseException;
        public ClassType BaseStruct;
        public ClassType BaseFunction;  // models functions, lambas and methods
        public ClassType BaseClass;  // models classes and instances

        public ClassType Datetime_datetime;
        public ClassType Datetime_date;
        public ClassType Datetime_time;
        public ClassType Datetime_timedelta;
        public ClassType Datetime_tzinfo;
        public InstanceType? Time_struct_time;



        public static string[] builtin_exception_types = {
            "ArithmeticError", "AssertionError", "AttributeError",
            "BaseException", "Exception", "DeprecationWarning", "EOFError",
            "EnvironmentError", "FloatingPointError", "FutureWarning",
            "GeneratorExit", "IOError", "ImportError", "ImportWarning",
            "IndentationError", "IndexError", "KeyError", "KeyboardInterrupt",
            "LookupError", "MemoryError", "NameError", "NotImplemented",
            "NotImplementedError", "OSError", "OverflowError",
            "PendingDeprecationWarning", "ReferenceError", "RuntimeError",
            "RuntimeWarning", "StandardError", "StopIteration", "SyntaxError",
            "SyntaxWarning", "SystemError", "SystemExit", "TabError",
            "TypeError", "UnboundLocalError", "UnicodeDecodeError",
            "UnicodeEncodeError", "UnicodeError", "UnicodeTranslateError",
            "UnicodeWarning", "UserWarning", "ValueError", "Warning",
            "ZeroDivisionError"
        };

        ClassType newClass(string name, NameScope table)
        {
            return newClass(name, table, null);
        }

        private ClassType newGenericClass(
            string name,
            int arity,
            NameScope scope,
            params ClassType[] superClasses)
        {
            var path = scope.ExtendPath(analyzer, name);
            var cls = ClassType.CreateUnboundGeneric(name, arity, scope, path);
            foreach (ClassType super in superClasses)
            {
                cls.AddSuper(super);
            }
            return cls;
        }

        private ClassType newClass(
            string name,
            NameScope scope,
            ClassType? superClass, 
            params ClassType[] moreSupers)
        {
            var path = scope.ExtendPath(analyzer, name);
            var cls = new ClassType(name, scope, path, superClass);
            foreach (ClassType super in moreSupers)
            {
                cls.AddSuper(super);
            }
            return cls;
        }

        ModuleType newModule(string name)
        {
            return new ModuleType(name, null, name, analyzer.GlobalTable);
        }

        ClassType newException(string name, NameScope t)
        {
            return newClass(name, t, BaseException);
        }

        FunType newFunc()
        {
            return new FunType();
        }

        FunType newFunc(DataType type)
        {
            if (type is null)
            {
                type = DataType.Unknown;
            }
            var fun = new FunType(DataType.Unknown, type);
            fun.Scope.AddSuperClass(analyzer.Builtins.BaseFunction.Scope);
            fun.Scope.Path = analyzer.Builtins.BaseFunction.Scope.Path;
            return fun;
        }

        ListType newList()
        {
            return newList(DataType.Unknown);
        }

        ListType newList(DataType type)
        {
            return analyzer.TypeFactory.CreateList(type);
        }

        DictType newDict(DataType ktype, DataType vtype)
        {
            return analyzer.TypeFactory.CreateDict(ktype, vtype);
        }

        TupleType newTuple(params DataType[] types)
        {
            return analyzer.TypeFactory.CreateTuple(types);
        }

        UnionType newUnion(params DataType[] types)
        {
            return new UnionType(types);
        }

        private abstract class NativeModule
        {
            protected Builtins outer;
            protected string name;
            protected ModuleType? module;
            protected NameScope table;  // the module's symbol table

            protected NativeModule(Builtins outer, string name)
            {
                this.outer = outer;
                this.name = name;
                this.table = default!;
                outer.modules[name] = this;
            }

            /// <summary>
            /// Lazily load the module.
            /// </summary>
            public ModuleType? getModule()
            {
                if (module is null)
                {
                    createModuleType();
                    initBindings();
                }
                return module;
            }

            public abstract void initBindings();

            protected void createModuleType()
            {
                if (module is null)
                {
                    module = outer.newModule(name);
                    table = module.Scope;
                    outer.analyzer.ModuleScope.AddExpressionBinding(outer.analyzer, name, liburl(), module, BindingKind.MODULE).IsBuiltin = true;
                }
            }
            
            protected void update(string name, Url url, DataType type, BindingKind kind)
            {
                table.AddExpressionBinding(outer.analyzer, name, url, type, kind).IsBuiltin = true;
            }

            protected void addClass(string name, Url url, DataType type)
            {
                table.AddExpressionBinding(outer.analyzer, name, url, type, BindingKind.CLASS).IsBuiltin = true;
            }

            protected void addMethod(string name, Url url, DataType type)
            {
                table.AddExpressionBinding(outer.analyzer, name, url, type, BindingKind.METHOD).IsBuiltin = true;
            }

            protected void addFunction(string name, Url url, DataType type)
            {
                table.AddExpressionBinding(outer.analyzer, name, url, outer.newFunc(type), BindingKind.FUNCTION).IsBuiltin = true;
            }

            // don't use this unless you're sure it's OK to share the type object
            protected void addFunctions_beCareful(DataType type, params string[] names)
            {
                foreach (string name in names)
                {
                    addFunction(name, liburl(), type);
                }
            }

            protected void addNoneFuncs(params string[] names)
            {
                addFunctions_beCareful(DataType.None, names);
            }

            protected void addNumFuncs(params string[] names)
            {
                addFunctions_beCareful(DataType.Int, names);
            }

            protected void addStrFuncs(params string[] names)
            {
                addFunctions_beCareful(DataType.Str, names);
            }

            protected void addUnknownFuncs(params string[] names)
            {
                foreach (string name in names)
                {
                    addFunction(name, liburl(), DataType.Unknown);
                }
            }

            protected void addAttr(string name, Url url, DataType type)
            {
                table.AddExpressionBinding(outer.analyzer, name, url, type, BindingKind.ATTRIBUTE).IsBuiltin = true;
            }


            // don't use this unless you're sure it's OK to share the type object
            protected void addAttributes_beCareful(DataType type, params string[] names)
            {
                foreach (string name in names)
                {
                    addAttr(name, liburl(), type);
                }
            }


            protected void addNumAttrs(params string[] names)
            {
                addAttributes_beCareful(DataType.Int, names);
            }


            protected void addStrAttrs(params string[] names)
            {
                addAttributes_beCareful(DataType.Str, names);
            }


            protected void addUnknownAttrs(params string[] names)
            {
                foreach (string name in names)
                {
                    addAttr(name, liburl(), DataType.Unknown);
                }
            }

            protected virtual Url liburl()
            {
                return newLibUrl(name);
            }

            protected virtual Url liburl(string anchor)
            {
                return newLibUrl(name, anchor);
            }

            public override string ToString()
            {
                return module == null
                        ? "<Non-loaded builtin module '" + name + "'>"
                        : "<NativeModule:" + module + ">";
            }
        }


        /// <summary>
        /// The set of top-level native modules.
        /// </summary>
        private IDictionary<string, NativeModule> modules = new Dictionary<string, NativeModule>();

#nullable disable
        public Builtins(AnalyzerImpl analyzer)
        {
            this.analyzer = analyzer;
            buildTypes();
        }
#nullable enable


        private void buildTypes()
        {
            new BuiltinsModule(this);
            NameScope bt = Builtin.Scope;

            objectType = newClass("object", bt);
            BaseType = newClass("type", bt, objectType);
            BaseTuple = newClass("tuple", bt, objectType);
            BaseList = newGenericClass("list", 1, bt, objectType);
            BaseListInst = new InstanceType(BaseList);
            BaseArray = newClass("array", bt);
            BaseDict = newClass("dict", bt, objectType);
            BaseIterable = newClass("iter", bt, objectType);
            ClassType numClass = newClass("int", bt, objectType);
            BaseStr = newClass("str", bt, objectType);
            BaseModule = newClass("module", bt);
            BaseFile = newClass("file", bt, objectType);
            BaseFileInst = new InstanceType(BaseFile);
            BaseFunction = newClass("function", bt, objectType);
            BaseClass = newClass("classobj", bt, objectType);
        }


        public void Initialize()
        {
            buildObjectType();
            buildTupleType();
            buildArrayType();
            buildListType();
            buildDictType();
            buildBoolType();
            buildNumTypes();
            buildStrType();
            buildModuleType();
            buildFileType();
            buildFunctionType();
            buildClassType();

            modules["__builtin__"].initBindings();  // eagerly load these bindings

            new ArrayModule(this);
            new AudioopModule(this);
            new BinasciiModule(this);
            new Bz2Module(this);
            new CPickleModule(this);
            new CStringIOModule(this);
            new CMathModule(this);
            new CollectionsModule(this);
            new CryptModule(this);
            new CTypesModule(this);
            new DatetimeModule(this);
            new DbmModule(this);
            new ErrnoModule(this);
            new ExceptionsModule(this);
            new FcntlModule(this);
            new FpectlModule(this);
            new GcModule(this);
            new GdbmModule(this);
            new GrpModule(this);
            new ImpModule(this);
            new ItertoolsModule(this);
            new MarshalModule(this);
            new MathModule(this);
            new Md5Module(this);
            new MmapModule(this);
            new NisModule(this);
            new OperatorModule(this);
            new OsModule(this);
            new ParserModule(this);
            new PosixModule(this);
            new PwdModule(this);
            new PyexpatModule(this);
            new ReadlineModule(this);
            new ResourceModule(this);
            new SelectModule(this);
            new SignalModule(this);
            new ShaModule(this);
            new SpwdModule(this);
            new StropModule(this);
            new StructModule(this);
            new SysModule(this);
            new SyslogModule(this);
            new TermiosModule(this);
            new ThreadModule(this);
            new TimeModule(this);
            new UnicodedataModule(this);
            new ZipimportModule(this);
            new ZlibModule(this);
        }


        /**
         * Loads (if necessary) and returns the specified built-in module.
         */

        public ModuleType? get(string name)
        {
            if (!name.Contains("."))
            {  // unqualified
                return getModule(name);
            }

            string[] mods = name.Split('\\', '.');
            DataType? type = getModule(mods[0]);
            if (type is null)
            {
                return null;
            }
            for (int i = 1; i < mods.Length; i++)
            {
                type = type.Scope.LookupTypeOf(mods[i]);
                if (type is not ModuleType)
                {
                    return null;
                }
            }
            return (ModuleType) type;
        }

        private ModuleType? getModule(string name)
        {
            if (!modules.TryGetValue(name, out var wrap))
                return null;
            else
                return wrap.getModule();
        }

        void buildObjectType()
        {
            string[] obj_methods = 
            {
                "__delattr__", "__format__", "__getattribute__", "__hash__",
                "__init__", "__new__", "__reduce__", "__reduce_ex__",
                "__repr__", "__setattr__", "__sizeof__", "__str__", "__subclasshook__"
            };
            foreach (string m in obj_methods)
            {
                objectType.Scope.AddExpressionBinding(analyzer, m, newLibUrl("stdtypes"), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            }
            objectType.Scope.AddExpressionBinding(analyzer, "__doc__", newLibUrl("stdtypes"), DataType.Str, BindingKind.CLASS).IsBuiltin = true;
            objectType.Scope.AddExpressionBinding(analyzer, "__class__", newLibUrl("stdtypes"), DataType.Unknown, BindingKind.CLASS).IsBuiltin = true;
        }

        void buildTupleType()
        {
            NameScope bt = BaseTuple.Scope;
            string[] tuple_methods = 
            {
                "__add__", "__contains__", "__eq__", "__ge__", "__getnewargs__",
                "__gt__", "__iter__", "__le__", "__len__", "__lt__", "__mul__",
                "__ne__", "__new__", "__rmul__", "count", "index"
            };
            foreach (string m in tuple_methods)
            {
                bt.AddExpressionBinding(analyzer, m, newLibUrl("stdtypes"), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            }
            bt.AddExpressionBinding(analyzer, "__getslice__", newDataModelUrl("object.__getslice__"), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            bt.AddExpressionBinding(analyzer, "__getitem__", newDataModelUrl("object.__getitem__"), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            bt.AddExpressionBinding(analyzer, "__iter__", newDataModelUrl("object.__iter__"), newFunc(), BindingKind.METHOD).IsBuiltin = true;
        }

        void buildArrayType()
        {
            string[] array_methods_none = 
            {
                "append", "buffer_info", "byteswap", "extend", "fromfile",
                "fromlist", "fromstring", "fromunicode", "index", "insert", "pop",
                "read", "remove", "reverse", "tofile", "tolist", "typecode", "write"
            };
            foreach (string m in array_methods_none)
            {
                BaseArray.Scope.AddExpressionBinding(analyzer, m, newLibUrl("array"), newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
            }
            string[] array_methods_num = { "count", "itemsize", };
            foreach (string m in array_methods_num)
            {
                BaseArray.Scope.AddExpressionBinding(analyzer, m, newLibUrl("array"), newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
            }
            string[] array_methods_str = { "tostring", "tounicode", };
            foreach (string m in array_methods_str)
            {
                BaseArray.Scope.AddExpressionBinding(analyzer, m, newLibUrl("array"), newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
            }
        }

        void buildListType()
        {
            BaseList.Scope.AddExpressionBinding(analyzer, "__getslice__", newDataModelUrl("object.__getslice__"),
                    newFunc(BaseListInst), BindingKind.METHOD).IsBuiltin = true;
            BaseList.Scope.AddExpressionBinding(analyzer, "__getitem__", newDataModelUrl("object.__getitem__"),
                    newFunc(BaseList), BindingKind.METHOD).IsBuiltin = true;
            BaseList.Scope.AddExpressionBinding(analyzer, "__iter__", newDataModelUrl("object.__iter__"),
                    newFunc(BaseList), BindingKind.METHOD).IsBuiltin = true;

            string[] list_methods_none = {
                "append", "extend", "index", "insert", "pop", "remove", "reverse", "sort"
             };
            foreach (string m in list_methods_none)
            {
                BaseList.Scope.AddExpressionBinding(analyzer, m, newLibUrl("stdtypes"), newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
            }
            string[] list_methods_num = { "count" };
            foreach (string m in list_methods_num)
            {
                BaseList.Scope.AddExpressionBinding(analyzer, m, newLibUrl("stdtypes"), newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
            }
            analyzer.GlobalTable.DataTypes.Add("List", newList());
        }

        Url numUrl()
        {
            return newLibUrl("stdtypes", "typesnumeric");
        }

        private void buildBoolType()
        {
            analyzer.GlobalTable.DataTypes.Add("bool", DataType.Bool);
        }

        void buildNumTypes()
        {
            NameScope bft = DataType.Float.Scope;
            string[] float_methods_num = {
                "__abs__", "__add__", "__coerce__", "__div__", "__divmod__",
                "__eq__", "__float__", "__floordiv__", "__format__",
                "__ge__", "__getformat__", "__gt__", "__int__",
                "__le__", "__long__", "__lt__", "__mod__", "__mul__", "__ne__",
                "__neg__", "__new__", "__nonzero__", "__pos__", "__pow__",
                "__radd__", "__rdiv__", "__rdivmod__", "__rfloordiv__", "__rmod__",
                "__rmul__", "__rpow__", "__rsub__", "__rtruediv__", "__setformat__",
                "__sub__", "__truediv__", "__trunc__", "as_integer_ratio",
                "fromhex", "is_integer"
        };
            foreach (string m in float_methods_num)
            {
                bft.AddExpressionBinding(analyzer, m, numUrl(), newFunc(DataType.Float), BindingKind.METHOD).IsBuiltin = true;
            }
            analyzer.GlobalTable.DataTypes.Add("float", DataType.Float);


            NameScope bnt = DataType.Int.Scope;
            string[] num_methods_num = {
                "__abs__", "__add__", "__and__",
                "__class__", "__cmp__", "__coerce__", "__delattr__", "__div__",
                "__divmod__", "__doc__", "__float__", "__floordiv__",
                "__getattribute__", "__getnewargs__", "__hash__", "__hex__",
                "__index__", "__init__", "__int__", "__invert__", "__long__",
                "__lshift__", "__mod__", "__mul__", "__neg__", "__new__",
                "__nonzero__", "__oct__", "__or__", "__pos__", "__pow__",
                "__radd__", "__rand__", "__rdiv__", "__rdivmod__",
                "__reduce__", "__reduce_ex__", "__repr__", "__rfloordiv__",
                "__rlshift__", "__rmod__", "__rmul__", "__ror__", "__rpow__",
                "__rrshift__", "__rshift__", "__rsub__", "__rtruediv__",
                "__rxor__", "__setattr__", "__str__", "__sub__", "__truediv__",
                "__xor__"
        };
            foreach (string m in num_methods_num)
            {
                bnt.AddExpressionBinding(analyzer, m, numUrl(), newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
            }
            bnt.AddExpressionBinding(analyzer, "__getnewargs__", numUrl(), newFunc(newTuple(DataType.Int)), BindingKind.METHOD).IsBuiltin = true;
            bnt.AddExpressionBinding(analyzer, "hex", numUrl(), newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
            bnt.AddExpressionBinding(analyzer, "conjugate", numUrl(), newFunc(DataType.Complex), BindingKind.METHOD).IsBuiltin = true;
            analyzer.GlobalTable.DataTypes.Add("int", DataType.Int);

            NameScope bct = DataType.Complex.Scope;
            string[] complex_methods = {
                "__abs__", "__add__", "__div__", "__divmod__",
                "__float__", "__floordiv__", "__format__", "__getformat__", "__int__",
                "__long__", "__mod__", "__mul__", "__neg__", "__new__",
                "__pos__", "__pow__", "__radd__", "__rdiv__", "__rdivmod__",
                "__rfloordiv__", "__rmod__", "__rmul__", "__rpow__", "__rsub__",
                "__rtruediv__", "__sub__", "__truediv__", "conjugate"
        };
            foreach (string c in complex_methods)
            {
                bct.AddExpressionBinding(analyzer, c, numUrl(), newFunc(DataType.Complex), BindingKind.METHOD).IsBuiltin = true;
            }
            string[] complex_methods_num = {
                "__eq__", "__ge__", "__gt__", "__le__", "__lt__", "__ne__",
                "__nonzero__", "__coerce__"
        };
            foreach (string cn in complex_methods_num)
            {
                bct.AddExpressionBinding(analyzer, cn, numUrl(), newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
            }
            bct.AddExpressionBinding(analyzer, "__getnewargs__", numUrl(), newFunc(newTuple(DataType.Complex)), BindingKind.METHOD).IsBuiltin = true;
            bct.AddExpressionBinding(analyzer, "imag", numUrl(), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
            bct.AddExpressionBinding(analyzer, "real", numUrl(), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
            analyzer.GlobalTable.DataTypes.Add("complex", DataType.Complex);

        }


        void buildStrType()
        {
            DataType.Str.Scope.AddExpressionBinding(analyzer, "__getslice__", newDataModelUrl("object.__getslice__"),
                    newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
            DataType.Str.Scope.AddExpressionBinding(analyzer, "__getitem__", newDataModelUrl("object.__getitem__"),
                    newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
            DataType.Str.Scope.AddExpressionBinding(analyzer, "__iter__", newDataModelUrl("object.__iter__"),
                    newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;

            string[] str_methods_str = {
                "capitalize", "center", "decode", "encode", "expandtabs", "format",
                "index", "join", "ljust", "lower", "lstrip", "partition", "replace",
                "rfind", "rindex", "rjust", "rpartition", "rsplit", "rstrip",
                "strip", "swapcase", "title", "translate", "upper", "zfill"
        };
            foreach (string m in str_methods_str)
            {
                DataType.Str.Scope.AddExpressionBinding(analyzer, m, newLibUrl("stdtypes.html#str." + m),
                        newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
            }

            string[] str_methods_num = {
                "count", "isalnum", "isalpha", "isdigit", "islower", "isspace",
                "istitle", "isupper", "find", "startswith", "endswith"
        };
            foreach (string m in str_methods_num)
            {
                DataType.Str.Scope.AddExpressionBinding(analyzer, m, newLibUrl("stdtypes.html#str." + m),
                        newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
            }

            string[] str_methods_list = { "split", "splitlines" };
            foreach (string m in str_methods_list)
            {
                DataType.Str.Scope.AddExpressionBinding(analyzer, m, newLibUrl("stdtypes.html#str." + m),
                        newFunc(newList(DataType.Str)), BindingKind.METHOD).IsBuiltin = true;
            }
            DataType.Str.Scope.AddExpressionBinding(analyzer, "partition", newLibUrl("stdtypes"),
                    newFunc(newTuple(DataType.Str)), BindingKind.METHOD).IsBuiltin = true;

            analyzer.GlobalTable.DataTypes.Add("str", DataType.Str);
        }


        void buildModuleType()
        {
            string[] attrs = { "__doc__", "__file__", "__name__", "__package__" };
            foreach (string m in attrs)
            {
                BaseModule.Scope.AddExpressionBinding(analyzer, m, newTutUrl("modules.html"), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
            }
            BaseModule.Scope.AddExpressionBinding(analyzer, "__dict__", newLibUrl("stdtypes", "modules"),
                    newDict(DataType.Str, DataType.Unknown), BindingKind.ATTRIBUTE).IsBuiltin = true;
        }


        void buildDictType()
        {
            string url = "datastructures.html#dictionaries";
            NameScope bt = BaseDict.Scope;

            bt.AddExpressionBinding(analyzer, "__getitem__", newTutUrl(url), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            bt.AddExpressionBinding(analyzer, "__iter__", newTutUrl(url), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            bt.AddExpressionBinding(analyzer, "get", newTutUrl(url), newFunc(), BindingKind.METHOD).IsBuiltin = true;

            bt.AddExpressionBinding(analyzer, "items", newTutUrl(url),
                    newFunc(newList(newTuple(DataType.Unknown, DataType.Unknown))), BindingKind.METHOD).IsBuiltin = true;

            bt.AddExpressionBinding(analyzer, "keys", newTutUrl(url), newFunc(BaseList), BindingKind.METHOD).IsBuiltin = true;
            bt.AddExpressionBinding(analyzer, "values", newTutUrl(url), newFunc(BaseList), BindingKind.METHOD).IsBuiltin = true;

            string[] dict_method_unknown = 
            {
                "clear", "copy", "fromkeys", "get", "iteritems", "iterkeys",
                "itervalues", "pop", "popitem", "setdefault", "update"
            };
            foreach (string m in dict_method_unknown)
            {
                bt.AddExpressionBinding(analyzer, m, newTutUrl(url), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            }

            string[] dict_method_num = { "has_key" };
            foreach (string m in dict_method_num)
            {
                bt.AddExpressionBinding(analyzer, m, newTutUrl(url), newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
            }
        }


        void buildFileType()
        {
            string url = "stdtypes.html#bltin-file-objects";
            NameScope table = BaseFile.Scope;

            string[] methods_unknown = {
                "__enter__", "__exit__", "__iter__", "flush", "readinto", "truncate"
        };
            foreach (string m in methods_unknown)
            {
                table.AddExpressionBinding(analyzer, m, newLibUrl(url), newFunc(), BindingKind.METHOD).IsBuiltin = true;
            }

            string[] methods_str = { "next", "read", "readline" };
            foreach (string m in methods_str)
            {
                table.AddExpressionBinding(analyzer, m, newLibUrl(url), newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
            }

            string[] num = { "fileno", "isatty", "tell" };
            foreach (string m in num)
            {
                table.AddExpressionBinding(analyzer, m, newLibUrl(url), newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
            }

            string[] methods_none = { "close", "seek", "write", "writelines" };
            foreach (string m in methods_none)
            {
                table.AddExpressionBinding(analyzer, m, newLibUrl(url), newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
            }

            table.AddExpressionBinding(analyzer, "readlines", newLibUrl(url), newFunc(newList(DataType.Str)), BindingKind.METHOD).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "xreadlines", newLibUrl(url), newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "closed", newLibUrl(url), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "encoding", newLibUrl(url), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "errors", newLibUrl(url), DataType.Unknown, BindingKind.ATTRIBUTE).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "mode", newLibUrl(url), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "name", newLibUrl(url), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "softspace", newLibUrl(url), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
            table.AddExpressionBinding(analyzer, "newlines", newLibUrl(url), newUnion(DataType.Str, newTuple(DataType.Str)), BindingKind.ATTRIBUTE).IsBuiltin = true;
        }

        void buildFunctionType()
        {
            NameScope t = BaseFunction.Scope;

            foreach (string s in new[] { "func_doc", "__doc__", "func_name", "__name__", "__module__" })
            {
                t.AddExpressionBinding(analyzer, s, new Url(DATAMODEL_URL), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
            }

            t.AddExpressionBinding(analyzer, "func_closure", new Url(DATAMODEL_URL), newTuple(), BindingKind.ATTRIBUTE).IsBuiltin = true;
            t.AddExpressionBinding(analyzer, "func_code", new Url(DATAMODEL_URL), DataType.Unknown, BindingKind.ATTRIBUTE).IsBuiltin = true;
            t.AddExpressionBinding(analyzer, "func_defaults", new Url(DATAMODEL_URL), newTuple(), BindingKind.ATTRIBUTE).IsBuiltin = true;
            t.AddExpressionBinding(analyzer, "func_globals", new Url(DATAMODEL_URL), analyzer.TypeFactory.CreateDict(DataType.Str, DataType.Unknown),
                    BindingKind.ATTRIBUTE).IsBuiltin = true;
            t.AddExpressionBinding(analyzer, "func_dict", new Url(DATAMODEL_URL), analyzer.TypeFactory.CreateDict(DataType.Str, DataType.Unknown), BindingKind.ATTRIBUTE).IsBuiltin = true;

            // Assume any function can become a method, for simplicity.
            foreach (string s in new[] { "__func__", "im_func" })
            {
                t.AddExpressionBinding(analyzer, s, new Url(DATAMODEL_URL), new FunType(), BindingKind.METHOD).IsBuiltin = true;
            }
        }


        // XXX:  finish wiring this up.  ClassType needs to inherit from it somehow,
        // so we can remove the per-instance attributes from NClassDef.
        void buildClassType()
        {
            NameScope t = BaseClass.Scope;

            foreach (string s in new[] { "__name__", "__doc__", "__module__" })
            {
                t.AddExpressionBinding(analyzer, s, new Url(DATAMODEL_URL), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
            }

            t.AddExpressionBinding(analyzer, "__dict__", new Url(DATAMODEL_URL), new DictType(DataType.Str, DataType.Unknown), BindingKind.ATTRIBUTE).IsBuiltin = true;
        }


        class BuiltinsModule : NativeModule
        {
            public BuiltinsModule(Builtins outer) :
                base(outer, "__builtin__")
            {
                outer.Builtin = module = outer.newModule(name);
                table = module.Scope;
            }

            public override void initBindings()
            {
                outer.analyzer.ModuleScope.AddExpressionBinding(outer.analyzer, name, liburl(), module!, BindingKind.MODULE).IsBuiltin = true;
                table.AddSuperClass(outer.BaseModule.Scope);

                addClass("None", newLibUrl("constants"), DataType.None);
                addFunction("bool", newLibUrl("functions", "bool"), DataType.Bool);
                addFunction("complex", newLibUrl("functions", "complex"), DataType.Complex);
                addClass("dict", newLibUrl("stdtypes", "typesmapping"), outer.BaseDict);
                addFunction("file", newLibUrl("functions", "file"), outer.BaseFileInst);
                addFunction("int", newLibUrl("functions", "int"), DataType.Int);
                addFunction("long", newLibUrl("functions", "long"), DataType.Int);
                addFunction("float", newLibUrl("functions", "float"), DataType.Float);
                addFunction("list", newLibUrl("functions", "list"), new InstanceType(outer.BaseList));
                addFunction("object", newLibUrl("functions", "object"), new InstanceType(outer.objectType));
                addFunction("str", newLibUrl("functions", "str"), DataType.Str);
                addFunction("tuple", newLibUrl("functions", "tuple"), new InstanceType(outer.BaseTuple));
                addFunction("type", newLibUrl("functions", "type"), new InstanceType(outer.BaseType));

                // XXX:  need to model the following as built-in class types:
                //   basestring, bool, buffer, frozenset, property, set, slice,
                //   staticmethod, super and unicode
                string[] builtin_func_unknown = {
                    "apply", "basestring", "callable", "classmethod",
                    "coerce", "compile", "copyright", "credits", "delattr", "enumerate",
                    "eval", "execfile", "exit", "filter", "frozenset", "getattr",
                    "help", "input", "intern", "iter", "license", "long",
                    "property", "quit", "raw_input", "reduce", "reload", "reversed",
                    "set", "setattr", "slice", "sorted", "staticmethod", "super",
                    "type", "unichr", "unicode",
            };
                foreach (string f in builtin_func_unknown)
                {
                    addFunction(f, newLibUrl("functions.html#" + f), DataType.Unknown);
                }

                string[] builtin_func_num = {
                    "abs", "all", "any", "cmp", "coerce", "divmod",
                    "hasattr", "hash", "id", "isinstance", "issubclass", "len", "max",
                    "min", "ord", "pow", "round", "sum"
            };
                foreach (string f in builtin_func_num)
                {
                    addFunction(f, newLibUrl("functions.html#" + f), DataType.Int);
                }

                foreach (string f in new[] { "hex", "oct", "repr", "chr" })
                {
                    addFunction(f, newLibUrl("functions.html#" + f), DataType.Str);
                }

                addFunction("dir", newLibUrl("functions", "dir"), outer.newList(DataType.Str));
                addFunction("map", newLibUrl("functions", "map"), outer.newList(DataType.Unknown));
                addFunction("range", newLibUrl("functions", "range"), outer.newList(DataType.Int));
                addFunction("xrange", newLibUrl("functions", "range"), outer.newList(DataType.Int));
                addFunction("buffer", newLibUrl("functions", "buffer"), outer.newList(DataType.Unknown));
                addFunction("zip", newLibUrl("functions", "zip"), outer.newList(outer.newTuple(DataType.Unknown)));


                foreach (string f in new[] { "globals", "vars", "locals" })
                {
                    addFunction(f, newLibUrl("functions.html#" + f), outer.newDict(DataType.Str, DataType.Unknown));
                }

                foreach (string f in builtin_exception_types)
                {
                    addClass(f, newDataModelUrl("org/yinwang/pysonar/types"),
                            outer.newClass(f, outer.analyzer.GlobalTable, outer.objectType));
                }
                outer.BaseException = (ClassType) table.LookupTypeOf("BaseException")!;

                foreach (string f in new[] { "True", "False" })
                {
                    addAttr(f, newDataModelUrl("org/yinwang/pysonar/types"), DataType.Bool);
                }

                addAttr("None", newDataModelUrl("org/yinwang/pysonar/types"), DataType.None);
                addFunction("open", newTutUrl("inputoutput.html#reading-and-writing-files"), outer.BaseFileInst);
                addFunction("__import__", newLibUrl("functions"), outer.newModule("<?>"));

                outer.analyzer.GlobalTable.AddExpressionBinding(outer.analyzer, "__builtins__", liburl(), module!, BindingKind.ATTRIBUTE).IsBuiltin = true;
                outer.analyzer.GlobalTable.AddAllBindings(table);
            }
        }


        class ArrayModule : NativeModule
        {
            public ArrayModule(Builtins outer) :
                base(outer, "array")
            {
            }


            public override void initBindings()
            {
                addClass("array", newLibUrl("array", "array"), outer.BaseArray);
                addClass("ArrayType", newLibUrl("array", "ArrayType"), outer.BaseArray);
            }
        }


        class AudioopModule : NativeModule
        {
            public AudioopModule(Builtins outer) :
                base(outer, "audioop")
            {
            }


            public override void initBindings()
            {
                addClass("error", liburl(), outer.newException("error", table));

                addStrFuncs("add", "adpcm2lin", "alaw2lin", "bias", "lin2alaw", "lin2lin",
                        "lin2ulaw", "mul", "reverse", "tomono", "ulaw2lin");

                addNumFuncs("avg", "avgpp", "cross", "findfactor", "findmax",
                        "getsample", "max", "maxpp", "rms");

                foreach (string s in new[] { "adpcm2lin", "findfit", "lin2adpcm", "minmax", "ratecv" })
                {
                    addFunction(s, liburl(), outer.newTuple());
                }
            }
        }


        class BinasciiModule : NativeModule
        {
            public BinasciiModule(Builtins outer) :
                base(outer, "binascii")
            {
            }


            public override void initBindings()
            {
                addStrFuncs(
                        "a2b_uu", "b2a_uu", "a2b_base64", "b2a_base64", "a2b_qp",
                        "b2a_qp", "a2b_hqx", "rledecode_hqx", "rlecode_hqx", "b2a_hqx",
                        "b2a_hex", "hexlify", "a2b_hex", "unhexlify");

                addNumFuncs("crc_hqx", "crc32");

                addClass("Error", liburl(), outer.newException("Error", table));
                addClass("Incomplete", liburl(), outer.newException("Incomplete", table));
            }
        }


        class Bz2Module : NativeModule
        {
            public Bz2Module(Builtins outer) : base(outer, "bz2") { }

            public override void initBindings()
            {
                ClassType bz2 = outer.newClass("BZ2File", table, outer.BaseFile);  // close enough.
                addClass("BZ2File", liburl(), bz2);

                ClassType bz2c = outer.newClass("BZ2Compressor", table, outer.objectType);
                bz2c.Scope.AddExpressionBinding(outer.analyzer, "compress", newLibUrl("bz2", "sequential-de-compression"),
                        outer.newFunc(DataType.Str), BindingKind.METHOD);
                bz2c.Scope.AddExpressionBinding(outer.analyzer, "flush", newLibUrl("bz2", "sequential-de-compression"),
                        outer.newFunc(DataType.None), BindingKind.METHOD);
                addClass("BZ2Compressor", newLibUrl("bz2", "sequential-de-compression"), bz2c);

                ClassType bz2d = outer.newClass("BZ2Decompressor", table, outer.objectType);
                bz2d.Scope.AddExpressionBinding(outer.analyzer, "decompress", newLibUrl("bz2", "sequential-de-compression"),
                        outer.newFunc(DataType.Str), BindingKind.METHOD);
                addClass("BZ2Decompressor", newLibUrl("bz2", "sequential-de-compression"), bz2d);

                addFunction("compress", newLibUrl("bz2", "one-shot-de-compression"), DataType.Str);
                addFunction("decompress", newLibUrl("bz2", "one-shot-de-compression"), DataType.Str);
            }
        }


        class CPickleModule : NativeModule
        {
            public CPickleModule(Builtins outer)
                : base(outer, "cPickle")
            {
            }

            protected override Url liburl()
            {
                return newLibUrl("pickle", "module-cPickle");
            }

            public override void initBindings()
            {
                addUnknownFuncs("dump", "load", "dumps", "loads");

                addClass("PickleError", liburl(), outer.newException("PickleError", table));

                ClassType picklingError = outer.newException("PicklingError", table);
                addClass("PicklingError", liburl(), picklingError);
                update("UnpickleableError", liburl(),
                        outer.newClass("UnpickleableError", table, picklingError), BindingKind.CLASS);
                ClassType unpicklingError = outer.newException("UnpicklingError", table);
                addClass("UnpicklingError", liburl(), unpicklingError);
                update("BadPickleGet", liburl(),
                        outer.newClass("BadPickleGet", table, unpicklingError), BindingKind.CLASS);

                ClassType pickler = outer.newClass("Pickler", table, outer.objectType);
                pickler.Scope.AddExpressionBinding(outer.analyzer, "dump", liburl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                pickler.Scope.AddExpressionBinding(outer.analyzer, "clear_memo", liburl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("Pickler", liburl(), pickler);

                ClassType unpickler = outer.newClass("Unpickler", table, outer.objectType);
                unpickler.Scope.AddExpressionBinding(outer.analyzer, "load", liburl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                unpickler.Scope.AddExpressionBinding(outer.analyzer, "noload", liburl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("Unpickler", liburl(), unpickler);
            }
        }


        class CStringIOModule : NativeModule
        {
            public CStringIOModule(Builtins outer)
                : base(outer, "cStringIO")
            {
            }

            protected override Url liburl()
            {
                return newLibUrl("stringio");
            }

            protected override Url liburl(string anchor)
            {
                return newLibUrl("stringio", anchor);
            }

            public override void initBindings()
            {
                ClassType StringIO = outer.newClass("StringIO", table, outer.BaseFile);
                addFunction("StringIO", liburl(), new InstanceType(StringIO));
                addAttr("InputType", liburl(), outer.BaseType);
                addAttr("OutputType", liburl(), outer.BaseType);
                addAttr("cStringIO_CAPI", liburl(), DataType.Unknown);
            }
        }


        class CMathModule : NativeModule
        {
            public CMathModule(Builtins outer)
                : base(outer, "cmath")
            {
            }

            public override void initBindings()
            {
                addFunction("phase", liburl("conversions-to-and-from-polar-coordinates"), DataType.Int);
                addFunction("polar", liburl("conversions-to-and-from-polar-coordinates"),
                        outer.newTuple(DataType.Int, DataType.Int));
                addFunction("rect", liburl("conversions-to-and-from-polar-coordinates"),
                        DataType.Complex);

                foreach (string plf in new[] { "exp", "log", "log10", "sqrt" })
                {
                    addFunction(plf, liburl("power-and-logarithmic-functions"), DataType.Int);
                }

                foreach (string tf in new[] { "acos", "asin", "atan", "cos", "sin", "tan" })
                {
                    addFunction(tf, liburl("trigonometric-functions"), DataType.Int);
                }

                foreach (string hf in new[] { "acosh", "asinh", "atanh", "cosh", "sinh", "tanh" })
                {
                    addFunction(hf, liburl("hyperbolic-functions"), DataType.Complex);
                }

                foreach (string cf in new[] { "isinf", "isnan" })
                {
                    addFunction(cf, liburl("classification-functions"), DataType.Bool);
                }

                foreach (string c in new[] { "pi", "e" })
                {
                    addAttr(c, liburl("constants"), DataType.Int);
                }
            }
        }


        class CollectionsModule : NativeModule
        {
            public CollectionsModule(Builtins outer) :
                base(outer, "collections")
            {
            }

            private Url abcUrl()
            {
                return liburl("abcs-abstract-base-classes");
            }



            private Url dequeUrl()
            {
                return liburl("deque-objects");
            }


            public override void initBindings()
            {
                ClassType callable = outer.newClass("Callable", table, outer.objectType);
                callable.Scope.AddExpressionBinding(outer.analyzer, "__call__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("Callable", abcUrl(), callable);

                ClassType iterableType = outer.newClass("Iterable", table, outer.objectType);
                iterableType.Scope.AddExpressionBinding(outer.analyzer, "__next__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                iterableType.Scope.AddExpressionBinding(outer.analyzer, "__iter__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("Iterable", abcUrl(), iterableType);

                ClassType Hashable = outer.newClass("Hashable", table, outer.objectType);
                Hashable.Scope.AddExpressionBinding(outer.analyzer, "__hash__", abcUrl(), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                addClass("Hashable", abcUrl(), Hashable);

                ClassType Sized = outer.newClass("Sized", table, outer.objectType);
                Sized.Scope.AddExpressionBinding(outer.analyzer, "__len__", abcUrl(), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                addClass("Sized", abcUrl(), Sized);

                ClassType containerType = outer.newClass("Container", table, outer.objectType);
                containerType.Scope.AddExpressionBinding(outer.analyzer, "__contains__", abcUrl(), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                addClass("Container", abcUrl(), containerType);

                ClassType iteratorType = outer.newClass("Iterator", table, iterableType);
                addClass("Iterator", abcUrl(), iteratorType);

                ClassType sequenceType = outer.newClass("Sequence", table, Sized, iterableType, containerType);
                sequenceType.Scope.AddExpressionBinding(outer.analyzer, "__getitem__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                sequenceType.Scope.AddExpressionBinding(outer.analyzer, "reversed", abcUrl(), outer.newFunc(sequenceType), BindingKind.METHOD).IsBuiltin = true;
                sequenceType.Scope.AddExpressionBinding(outer.analyzer, "index", abcUrl(), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                sequenceType.Scope.AddExpressionBinding(outer.analyzer, "count", abcUrl(), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                addClass("Sequence", abcUrl(), sequenceType);

                ClassType mutableSequence = outer.newClass("MutableSequence", table, sequenceType);
                mutableSequence.Scope.AddExpressionBinding(outer.analyzer, "__setitem__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                mutableSequence.Scope.AddExpressionBinding(outer.analyzer, "__delitem__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("MutableSequence", abcUrl(), mutableSequence);

                ClassType setType = outer.newClass("Set", table, Sized, iterableType, containerType);
                setType.Scope.AddExpressionBinding(outer.analyzer, "__getitem__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("Set", abcUrl(), setType);

                ClassType mutableSet = outer.newClass("MutableSet", table, setType);
                mutableSet.Scope.AddExpressionBinding(outer.analyzer, "add", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                mutableSet.Scope.AddExpressionBinding(outer.analyzer, "discard", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("MutableSet", abcUrl(), mutableSet);

                ClassType mapping = outer.newClass("Mapping", table, Sized, iterableType, containerType);
                mapping.Scope.AddExpressionBinding(outer.analyzer, "__getitem__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("Mapping", abcUrl(), mapping);

                ClassType mutableMapping = outer.newClass("MutableMapping", table, mapping);
                mutableMapping.Scope.AddExpressionBinding(outer.analyzer, "__setitem__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                mutableMapping.Scope.AddExpressionBinding(outer.analyzer, "__delitem__", abcUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                addClass("MutableMapping", abcUrl(), mutableMapping);

                ClassType MappingView = outer.newClass("MappingView", table, Sized);
                addClass("MappingView", abcUrl(), MappingView);

                ClassType KeysView = outer.newClass("KeysView", table, Sized);
                addClass("KeysView", abcUrl(), KeysView);

                ClassType ItemsView = outer.newClass("ItemsView", table, Sized);
                addClass("ItemsView", abcUrl(), ItemsView);

                ClassType ValuesView = outer.newClass("ValuesView", table, Sized);
                addClass("ValuesView", abcUrl(), ValuesView);

                ClassType deque = outer.newClass("deque", table, outer.objectType);
                foreach (string n in new[] {"append", "appendLeft", "clear",
                        "extend", "extendLeft", "rotate"})
                {
                    deque.Scope.AddExpressionBinding(outer.analyzer, n, dequeUrl(), outer.newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
                }
                foreach (string u in new[] {"__getitem__", "__iter__",
                        "pop", "popleft", "remove"})
                {
                    deque.Scope.AddExpressionBinding(outer.analyzer, u, dequeUrl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                }
                addClass("deque", dequeUrl(), deque);

                ClassType defaultdict = outer.newClass("defaultdict", table, outer.objectType);
                defaultdict.Scope.AddExpressionBinding(outer.analyzer, "__missing__", liburl("defaultdict-objects"),
                        outer.newFunc(), BindingKind.METHOD);
                defaultdict.Scope.AddExpressionBinding(outer.analyzer, "default_factory", liburl("defaultdict-objects"),
                        outer.newFunc(), BindingKind.METHOD);
                addClass("defaultdict", liburl("defaultdict-objects"), defaultdict);

                string argh = "namedtuple-factory-function-for-tuples-with-named-fields";
                ClassType namedtuple = outer.newClass("(namedtuple)", table, outer.BaseTuple);
                namedtuple.Scope.AddExpressionBinding(outer.analyzer, "_fields", liburl(argh),
                        outer.newList(DataType.Str), BindingKind.ATTRIBUTE);
                addFunction("namedtuple", liburl(argh), namedtuple);
            }
        }


        class CTypesModule : NativeModule
        {
            public CTypesModule(Builtins outer)
                : base(outer, "ctypes")
            {
            }

            public override void initBindings()
            {
                string[] ctypes_attrs = {
                    "ARRAY", "ArgumentError", "Array", "BigEndianStructure", "CDLL",
                    "CFUNCTYPE", "DEFAULT_MODE", "DllCanUnloadNow", "DllGetClassObject",
                    "FormatError", "GetLastError", "HRESULT", "LibraryLoader",
                    "LittleEndianStructure", "OleDLL", "POINTER", "PYFUNCTYPE", "PyDLL",
                    "RTLD_GLOBAL", "RTLD_LOCAL", "SetPointerType", "Structure", "Union",
                    "WINFUNCTYPE", "WinDLL", "WinError", "_CFuncPtr", "_FUNCFLAG_CDECL",
                    "_FUNCFLAG_PYTHONAPI", "_FUNCFLAG_STDCALL", "_FUNCFLAG_USE_ERRNO",
                    "_FUNCFLAG_USE_LASTERROR", "_Pointer", "_SimpleCData",
                    "_c_functype_cache", "_calcsize", "_cast", "_cast_addr",
                    "_check_HRESULT", "_check_size", "_ctypes_version", "_dlopen",
                    "_endian", "_memmove_addr", "_memset_addr", "_os",
                    "_pointer_type_cache", "_string_at", "_string_at_addr", "_sys",
                    "_win_functype_cache", "_wstring_at", "_wstring_at_addr",
                    "addressof", "alignment", "byref", "c_bool", "c_buffer", "c_byte",
                    "c_char", "c_char_p", "c_double", "c_float", "c_int", "c_int16",
                    "c_int32", "c_int64", "c_int8", "c_long", "c_longdouble",
                    "c_longlong", "c_short", "c_size_t", "c_ubyte", "c_uint",
                    "c_uint16", "c_uint32", "c_uint64", "c_uint8", "c_ulong",
                    "c_ulonglong", "c_ushort", "c_void_p", "c_voidp", "c_wchar",
                    "c_wchar_p", "cast", "cdll", "create_string_buffer",
                    "create_unicode_buffer", "get_errno", "get_last_error", "memmove",
                    "memset", "oledll", "pointer", "py_object", "pydll", "pythonapi",
                    "resize", "set_conversion_mode", "set_errno", "set_last_error",
                    "sizeof", "string_at", "windll", "wstring_at"
            };
                foreach (string attr in ctypes_attrs)
                {
                    addAttr(attr, liburl(attr), DataType.Unknown);
                }
            }
        }


        class CryptModule : NativeModule
        {
            public CryptModule(Builtins outer)
                : base(outer, "crypt")
            {
            }


            public override void initBindings()
            {
                addStrFuncs("crypt");
            }
        }


        class DatetimeModule : NativeModule
        {
            public DatetimeModule(Builtins outer)
                : base(outer, "datetime")
            {
            }

            private Url dtUrl(string anchor)
            {
                return liburl("datetime." + anchor);
            }


            public override void initBindings()
            {
                // XXX:  make datetime, time, date, timedelta and tzinfo Base* objects,
                // so built-in functions can return them.

                addNumAttrs("MINYEAR", "MAXYEAR");

                ClassType timedelta = outer.Datetime_timedelta = outer.newClass("timedelta", table, outer.objectType);
                addClass("timedelta", dtUrl("timedelta"), timedelta);
                NameScope tdtable = outer.Datetime_timedelta.Scope;
                tdtable.AddExpressionBinding(outer.analyzer, "min", dtUrl("timedelta"), timedelta, BindingKind.ATTRIBUTE).IsBuiltin = true;
                tdtable.AddExpressionBinding(outer.analyzer, "max", dtUrl("timedelta"), timedelta, BindingKind.ATTRIBUTE).IsBuiltin = true;
                tdtable.AddExpressionBinding(outer.analyzer, "resolution", dtUrl("timedelta"), timedelta, BindingKind.ATTRIBUTE).IsBuiltin = true;

                tdtable.AddExpressionBinding(outer.analyzer, "days", dtUrl("timedelta"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                tdtable.AddExpressionBinding(outer.analyzer, "seconds", dtUrl("timedelta"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                tdtable.AddExpressionBinding(outer.analyzer, "microseconds", dtUrl("timedelta"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;

                ClassType tzinfo = outer.Datetime_tzinfo = outer.newClass("tzinfo", table, outer.objectType);
                addClass("tzinfo", dtUrl("tzinfo"), tzinfo);
                NameScope tztable = outer.Datetime_tzinfo.Scope;
                tztable.AddExpressionBinding(outer.analyzer, "utcoffset", dtUrl("tzinfo"), outer.newFunc(timedelta), BindingKind.METHOD).IsBuiltin = true;
                tztable.AddExpressionBinding(outer.analyzer, "dst", dtUrl("tzinfo"), outer.newFunc(timedelta), BindingKind.METHOD).IsBuiltin = true;
                tztable.AddExpressionBinding(outer.analyzer, "tzname", dtUrl("tzinfo"), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                tztable.AddExpressionBinding(outer.analyzer, "fromutc", dtUrl("tzinfo"), outer.newFunc(tzinfo), BindingKind.METHOD).IsBuiltin = true;

                ClassType date = outer.Datetime_date = outer.newClass("date", table, outer.objectType);
                addClass("date", dtUrl("date"), date);
                NameScope dtable = outer.Datetime_date.Scope;
                dtable.AddExpressionBinding(outer.analyzer, "min", dtUrl("date"), date, BindingKind.ATTRIBUTE).IsBuiltin = true;
                dtable.AddExpressionBinding(outer.analyzer, "max", dtUrl("date"), date, BindingKind.ATTRIBUTE).IsBuiltin = true;
                dtable.AddExpressionBinding(outer.analyzer, "resolution", dtUrl("date"), timedelta, BindingKind.ATTRIBUTE).IsBuiltin = true;

                dtable.AddExpressionBinding(outer.analyzer, "today", dtUrl("date"), outer.newFunc(date), BindingKind.METHOD).IsBuiltin = true;
                dtable.AddExpressionBinding(outer.analyzer, "fromtimestamp", dtUrl("date"), outer.newFunc(date), BindingKind.METHOD).IsBuiltin = true;
                dtable.AddExpressionBinding(outer.analyzer, "fromordinal", dtUrl("date"), outer.newFunc(date), BindingKind.METHOD).IsBuiltin = true;

                dtable.AddExpressionBinding(outer.analyzer, "year", dtUrl("date"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                dtable.AddExpressionBinding(outer.analyzer, "month", dtUrl("date"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                dtable.AddExpressionBinding(outer.analyzer, "day", dtUrl("date"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;

                dtable.AddExpressionBinding(outer.analyzer, "replace", dtUrl("date"), outer.newFunc(date), BindingKind.METHOD).IsBuiltin = true;
                dtable.AddExpressionBinding(outer.analyzer, "timetuple", dtUrl("date"), outer.newFunc(outer.Time_struct_time!), BindingKind.METHOD).IsBuiltin = true;

                foreach (string n in new[] { "toordinal", "weekday", "isoweekday" })
                {
                    dtable.AddExpressionBinding(outer.analyzer, n, dtUrl("date"), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                }
                foreach (string r in new[] { "ctime", "strftime", "isoformat" })
                {
                    dtable.AddExpressionBinding(outer.analyzer, r, dtUrl("date"), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                }
                dtable.AddExpressionBinding(outer.analyzer, "isocalendar", dtUrl("date"),
                        outer.newFunc(outer.newTuple(DataType.Int, DataType.Int, DataType.Int)), BindingKind.METHOD);

                ClassType time = outer.Datetime_time = outer.newClass("time", table, outer.objectType);
                addClass("time", dtUrl("time"), time);
                NameScope ttable = outer.Datetime_time.Scope;

                ttable.AddExpressionBinding(outer.analyzer, "min", dtUrl("time"), time, BindingKind.ATTRIBUTE).IsBuiltin = true;
                ttable.AddExpressionBinding(outer.analyzer, "max", dtUrl("time"), time, BindingKind.ATTRIBUTE).IsBuiltin = true;
                ttable.AddExpressionBinding(outer.analyzer, "resolution", dtUrl("time"), timedelta, BindingKind.ATTRIBUTE).IsBuiltin = true;

                ttable.AddExpressionBinding(outer.analyzer, "hour", dtUrl("time"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                ttable.AddExpressionBinding(outer.analyzer, "minute", dtUrl("time"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                ttable.AddExpressionBinding(outer.analyzer, "second", dtUrl("time"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                ttable.AddExpressionBinding(outer.analyzer, "microsecond", dtUrl("time"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                ttable.AddExpressionBinding(outer.analyzer, "tzinfo", dtUrl("time"), tzinfo, BindingKind.ATTRIBUTE).IsBuiltin = true;

                ttable.AddExpressionBinding(outer.analyzer, "replace", dtUrl("time"), outer.newFunc(time), BindingKind.METHOD).IsBuiltin = true;

                foreach (string l in new[] { "isoformat", "strftime", "tzname" })
                {
                    ttable.AddExpressionBinding(outer.analyzer, l, dtUrl("time"), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                }
                foreach (string f in new[] { "utcoffset", "dst" })
                {
                    ttable.AddExpressionBinding(outer.analyzer, f, dtUrl("time"), outer.newFunc(timedelta), BindingKind.METHOD).IsBuiltin = true;
                }

                ClassType datetime = outer.Datetime_datetime = outer.newClass("datetime", table, date, time);
                addClass("datetime", dtUrl("datetime"), datetime);
                NameScope dttable = outer.Datetime_datetime.Scope;

                foreach (string c in new[] {"combine", "fromordinal", "fromtimestamp", "now",
                        "strptime", "today", "utcfromtimestamp", "utcnow"})
                {
                    dttable.AddExpressionBinding(outer.analyzer, c, dtUrl("datetime"), outer.newFunc(datetime), BindingKind.METHOD).IsBuiltin = true;
                }

                dttable.AddExpressionBinding(outer.analyzer, "min", dtUrl("datetime"), datetime, BindingKind.ATTRIBUTE).IsBuiltin = true;
                dttable.AddExpressionBinding(outer.analyzer, "max", dtUrl("datetime"), datetime, BindingKind.ATTRIBUTE).IsBuiltin = true;
                dttable.AddExpressionBinding(outer.analyzer, "resolution", dtUrl("datetime"), timedelta, BindingKind.ATTRIBUTE).IsBuiltin = true;

                dttable.AddExpressionBinding(outer.analyzer, "date", dtUrl("datetime"), outer.newFunc(date), BindingKind.METHOD).IsBuiltin = true;

                foreach (string x in new[] { "time", "timetz" })
                {
                    dttable.AddExpressionBinding(outer.analyzer, x, dtUrl("datetime"), outer.newFunc(time), BindingKind.METHOD).IsBuiltin = true;
                }

                foreach (string y in new[] { "replace", "astimezone" })
                {
                    dttable.AddExpressionBinding(outer.analyzer, y, dtUrl("datetime"), outer.newFunc(datetime), BindingKind.METHOD).IsBuiltin = true;
                }

                dttable.AddExpressionBinding(outer.analyzer, "utctimetuple", dtUrl("datetime"), outer.newFunc(outer.Time_struct_time!), BindingKind.METHOD).IsBuiltin = true;
            }
        }


        class DbmModule : NativeModule
        {
            public DbmModule(Builtins outer) : base(outer, "dbm") { }

            public override void initBindings()
            {
                var path = table.ExtendPath(outer.analyzer, "dbm");
                ClassType dbm = new ClassType("dbm", table, path, outer.BaseDict);
                addClass("dbm", liburl(), dbm);
                addClass("error", liburl(), outer.newException("error", table));
                addStrAttrs("library");
                addFunction("open", liburl(), dbm);
            }
        }


        class ErrnoModule : NativeModule
        {
            public ErrnoModule(Builtins outer)
                : base(outer, "errno")
            {
            }

            public override void initBindings()
            {
                addNumAttrs(
                        "E2BIG", "EACCES", "EADDRINUSE", "EADDRNOTAVAIL", "EAFNOSUPPORT",
                        "EAGAIN", "EALREADY", "EBADF", "EBUSY", "ECHILD", "ECONNABORTED",
                        "ECONNREFUSED", "ECONNRESET", "EDEADLK", "EDEADLOCK",
                        "EDESTADDRREQ", "EDOM", "EDQUOT", "EEXIST", "EFAULT", "EFBIG",
                        "EHOSTDOWN", "EHOSTUNREACH", "EILSEQ", "EINPROGRESS", "EINTR",
                        "EINVAL", "EIO", "EISCONN", "EISDIR", "ELOOP", "EMFILE", "EMLINK",
                        "EMSGSIZE", "ENAMETOOLONG", "ENETDOWN", "ENETRESET", "ENETUNREACH",
                        "ENFILE", "ENOBUFS", "ENODEV", "ENOENT", "ENOEXEC", "ENOLCK",
                        "ENOMEM", "ENOPROTOOPT", "ENOSPC", "ENOSYS", "ENOTCONN", "ENOTDIR",
                        "ENOTEMPTY", "ENOTSOCK", "ENOTTY", "ENXIO", "EOPNOTSUPP", "EPERM",
                        "EPFNOSUPPORT", "EPIPE", "EPROTONOSUPPORT", "EPROTOTYPE", "ERANGE",
                        "EREMOTE", "EROFS", "ESHUTDOWN", "ESOCKTNOSUPPORT", "ESPIPE",
                        "ESRCH", "ESTALE", "ETIMEDOUT", "ETOOMANYREFS", "EUSERS",
                        "EWOULDBLOCK", "EXDEV", "WSABASEERR", "WSAEACCES", "WSAEADDRINUSE",
                        "WSAEADDRNOTAVAIL", "WSAEAFNOSUPPORT", "WSAEALREADY", "WSAEBADF",
                        "WSAECONNABORTED", "WSAECONNREFUSED", "WSAECONNRESET",
                        "WSAEDESTADDRREQ", "WSAEDISCON", "WSAEDQUOT", "WSAEFAULT",
                        "WSAEHOSTDOWN", "WSAEHOSTUNREACH", "WSAEINPROGRESS", "WSAEINTR",
                        "WSAEINVAL", "WSAEISCONN", "WSAELOOP", "WSAEMFILE", "WSAEMSGSIZE",
                        "WSAENAMETOOLONG", "WSAENETDOWN", "WSAENETRESET", "WSAENETUNREACH",
                        "WSAENOBUFS", "WSAENOPROTOOPT", "WSAENOTCONN", "WSAENOTEMPTY",
                        "WSAENOTSOCK", "WSAEOPNOTSUPP", "WSAEPFNOSUPPORT", "WSAEPROCLIM",
                        "WSAEPROTONOSUPPORT", "WSAEPROTOTYPE", "WSAEREMOTE", "WSAESHUTDOWN",
                        "WSAESOCKTNOSUPPORT", "WSAESTALE", "WSAETIMEDOUT",
                        "WSAETOOMANYREFS", "WSAEUSERS", "WSAEWOULDBLOCK",
                        "WSANOTINITIALISED", "WSASYSNOTREADY", "WSAVERNOTSUPPORTED");

                addAttr("errorcode", liburl("errorcode"), outer.newDict(DataType.Int, DataType.Str));
            }
        }


        class ExceptionsModule : NativeModule
        {
            public ExceptionsModule(Builtins outer) :
                base(outer, "exceptions")
            {
            }


            public override void initBindings()
            {
                ModuleType? builtins = outer.get("__builtin__");
                foreach (string s in builtin_exception_types)
                {
                    //                Binding b = builtins.getTable().lookup(s);
                    //                table.update(b.getName(), b.getFirstNode(), b.getType(), b.getKind());
                }
            }
        }


        class FcntlModule : NativeModule
        {
            public FcntlModule(Builtins outer)
                : base(outer, "fcntl")
            {
            }

            public override void initBindings()
            {
                foreach (string s in new[] { "fcntl", "ioctl" })
                {
                    addFunction(s, liburl(), outer.newUnion(DataType.Int, DataType.Str));
                }
                addNumFuncs("flock");
                addUnknownFuncs("lockf");

                addNumAttrs(
                        "DN_ACCESS", "DN_ATTRIB", "DN_CREATE", "DN_DELETE", "DN_MODIFY",
                        "DN_MULTISHOT", "DN_RENAME", "FASYNC", "FD_CLOEXEC", "F_DUPFD",
                        "F_EXLCK", "F_GETFD", "F_GETFL", "F_GETLEASE", "F_GETLK", "F_GETLK64",
                        "F_GETOWN", "F_GETSIG", "F_NOTIFY", "F_RDLCK", "F_SETFD", "F_SETFL",
                        "F_SETLEASE", "F_SETLK", "F_SETLK64", "F_SETLKW", "F_SETLKW64",
                        "F_SETOWN", "F_SETSIG", "F_SHLCK", "F_UNLCK", "F_WRLCK", "I_ATMARK",
                        "I_CANPUT", "I_CKBAND", "I_FDINSERT", "I_FIND", "I_FLUSH",
                        "I_FLUSHBAND", "I_GETBAND", "I_GETCLTIME", "I_GETSIG", "I_GRDOPT",
                        "I_GWROPT", "I_LINK", "I_LIST", "I_LOOK", "I_NREAD", "I_PEEK",
                        "I_PLINK", "I_POP", "I_PUNLINK", "I_PUSH", "I_RECVFD", "I_SENDFD",
                        "I_SETCLTIME", "I_SETSIG", "I_SRDOPT", "I_STR", "I_SWROPT",
                        "I_UNLINK", "LOCK_EX", "LOCK_MAND", "LOCK_NB", "LOCK_READ", "LOCK_RW",
                        "LOCK_SH", "LOCK_UN", "LOCK_WRITE");
            }
        }


        class FpectlModule : NativeModule
        {
            public FpectlModule(Builtins outer)
                : base(outer, "fpectl")
            {
            }


            public override void initBindings()
            {
                addNoneFuncs("turnon_sigfpe", "turnoff_sigfpe");
                addClass("FloatingPointError", liburl(), outer.newException("FloatingPointError", table));
            }
        }


        class GcModule : NativeModule
        {
            public GcModule(Builtins outer)
                : base(outer, "gc")
            {
            }


            public override void initBindings()
            {
                addNoneFuncs("enable", "disable", "set_debug", "set_threshold");
                addNumFuncs("isenabled", "collect", "get_debug", "get_count", "get_threshold");
                foreach (string s in new[] { "get_objects", "get_referrers", "get_referents" })
                {
                    addFunction(s, liburl(), outer.newList());
                }
                addAttr("garbage", liburl(), outer.newList());
                addNumAttrs("DEBUG_STATS", "DEBUG_COLLECTABLE", "DEBUG_UNCOLLECTABLE",
                        "DEBUG_INSTANCES", "DEBUG_OBJECTS", "DEBUG_SAVEALL", "DEBUG_LEAK");
            }
        }


        class GdbmModule : NativeModule
        {
            public GdbmModule(Builtins outer)
                : base(outer, "gbdm")
            {
            }


            public override void initBindings()
            {
                addClass("error", liburl(), outer.newException("error", table));

                var path = table.ExtendPath(outer.analyzer, name);
                ClassType gdbm = new ClassType("gdbm", table, path, outer.BaseDict);
                gdbm.Scope.AddExpressionBinding(outer.analyzer, "firstkey", liburl(), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                gdbm.Scope.AddExpressionBinding(outer.analyzer, "nextkey", liburl(), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                gdbm.Scope.AddExpressionBinding(outer.analyzer, "reorganize", liburl(), outer.newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
                gdbm.Scope.AddExpressionBinding(outer.analyzer, "sync", liburl(), outer.newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;

                addFunction("open", liburl(), gdbm);
            }

        }


        class GrpModule : NativeModule
        {
            public GrpModule(Builtins outer)
                : base(outer, "grp")
            {
                this.outer = outer;
            }


            public override void initBindings()
            {
                outer.get("struct");
                ClassType struct_group = outer.newClass("struct_group", table, outer.BaseStruct);
                struct_group.Scope.AddExpressionBinding(outer.analyzer, "gr_name", liburl(), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
                struct_group.Scope.AddExpressionBinding(outer.analyzer, "gr_passwd", liburl(), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
                struct_group.Scope.AddExpressionBinding(outer.analyzer, "gr_gid", liburl(), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                struct_group.Scope.AddExpressionBinding(outer.analyzer, "gr_mem", liburl(), outer.newList(DataType.Str), BindingKind.ATTRIBUTE).IsBuiltin = true;

                addClass("struct_group", liburl(), struct_group);

                foreach (string s in new[] { "getgrgid", "getgrnam" })
                {
                    addFunction(s, liburl(), struct_group);
                }
                addFunction("getgrall", liburl(), outer.newList(struct_group));
            }
        }


        class ImpModule : NativeModule
        {
            public ImpModule(Builtins outer)
                : base(outer, "imp")
            {
            }

            public override void initBindings()
            {
                addStrFuncs("get_magic");
                addFunction("get_suffixes", liburl(), outer.newList(outer.newTuple(DataType.Str, DataType.Str, DataType.Int)));
                addFunction("find_module", liburl(), outer.newTuple(DataType.Str, DataType.Str, DataType.Int));

                string[] module_methods = {
                    "load_module", "new_module", "init_builtin", "init_frozen",
                    "load_compiled", "load_dynamic", "load_source"
            };
                foreach (string mm in module_methods)
                {
                    addFunction(mm, liburl(), outer.newModule("<?>"));
                }

                addUnknownFuncs("acquire_lock", "release_lock");

                addNumAttrs("PY_SOURCE", "PY_COMPILED", "C_EXTENSION",
                        "PKG_DIRECTORY", "C_BUILTIN", "PY_FROZEN", "SEARCH_ERROR");

                addNumFuncs("lock_held", "is_builtin", "is_frozen");

                ClassType impNullImporter = outer.newClass("NullImporter", table, outer.objectType);
                impNullImporter.Scope.AddExpressionBinding(outer.analyzer, "find_module", liburl(), outer.newFunc(DataType.None), BindingKind.FUNCTION).IsBuiltin = true;
                addClass("NullImporter", liburl(), impNullImporter);
            }
        }


        class ItertoolsModule : NativeModule
        {
            public ItertoolsModule(Builtins outer)
                : base(outer, "itertools")
            {
            }


            public override void initBindings()
            {
                ClassType iterator = outer.newClass("iterator", table, outer.objectType);
                iterator.Scope.AddExpressionBinding(outer.analyzer, "from_iterable", liburl("itertool-functions"),
                        outer.newFunc(iterator), BindingKind.METHOD);
                iterator.Scope.AddExpressionBinding(outer.analyzer, "next", liburl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;

                foreach (string s in new[] {"chain", "combinations", "count", "cycle",
                        "dropwhile", "groupby", "ifilter",
                        "ifilterfalse", "imap", "islice", "izip",
                        "izip_longest", "permutations", "product",
                        "repeat", "starmap", "takewhile", "tee"})
                {
                    addClass(s, liburl("itertool-functions"), iterator);
                }
            }
        }


        class MarshalModule : NativeModule
        {
            public MarshalModule(Builtins outer)
                : base(outer, "marshal")
            {
            }


            public override void initBindings()
            {
                addNumAttrs("version");
                addStrFuncs("dumps");
                addUnknownFuncs("dump", "load", "loads");
            }
        }


        class MathModule : NativeModule
        {
            public MathModule(Builtins outer)
                : base(outer, "math")
            {
            }


            public override void initBindings()
            {
                addNumFuncs(
                        "acos", "acosh", "asin", "asinh", "atan", "atan2", "atanh", "ceil",
                        "copysign", "cos", "cosh", "degrees", "exp", "fabs", "factorial",
                        "floor", "fmod", "frexp", "fsum", "hypot", "isinf", "isnan",
                        "ldexp", "log", "log10", "log1p", "modf", "pow", "radians", "sin",
                        "sinh", "sqrt", "tan", "tanh", "trunc");
                addNumAttrs("pi", "e");
            }
        }


        class Md5Module : NativeModule
        {
            public Md5Module(Builtins outer)
                : base(outer, "md5")
            {
            }


            public override void initBindings()
            {
                addNumAttrs("blocksize", "digest_size");

                ClassType md5 = outer.newClass("md5", table, outer.objectType);
                md5.Scope.AddExpressionBinding(outer.analyzer, "update", liburl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                md5.Scope.AddExpressionBinding(outer.analyzer, "digest", liburl(), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                md5.Scope.AddExpressionBinding(outer.analyzer, "hexdigest", liburl(), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                md5.Scope.AddExpressionBinding(outer.analyzer, "copy", liburl(), outer.newFunc(md5), BindingKind.METHOD).IsBuiltin = true;

                update("new", liburl(), outer.newFunc(md5), BindingKind.CONSTRUCTOR);
                update("md5", liburl(), outer.newFunc(md5), BindingKind.CONSTRUCTOR);
            }
        }


        class MmapModule : NativeModule
        {
            public MmapModule(Builtins outer)
                : base(outer, "mmap")
            {
            }
            public override void initBindings()
            {
                ClassType mmap = outer.newClass("mmap", table, outer.objectType);

                foreach (string s in new[]{"ACCESS_COPY", "ACCESS_READ", "ACCESS_WRITE",
                        "ALLOCATIONGRANULARITY", "MAP_ANON", "MAP_ANONYMOUS",
                        "MAP_DENYWRITE", "MAP_EXECUTABLE", "MAP_PRIVATE",
                        "MAP_SHARED", "PAGESIZE", "PROT_EXEC", "PROT_READ",
                        "PROT_WRITE"})
                {
                    mmap.Scope.AddExpressionBinding(outer.analyzer, s, liburl(), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }

                foreach (string fstr in new[] { "read", "read_byte", "readline" })
                {
                    mmap.Scope.AddExpressionBinding(outer.analyzer, fstr, liburl(), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                }

                foreach (string fnum in new[] { "find", "rfind", "tell" })
                {
                    mmap.Scope.AddExpressionBinding(outer.analyzer, fnum, liburl(), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                }

                foreach (string fnone in new[] {"close", "flush", "move", "resize", "seek",
                        "write", "write_byte"})
                {
                    mmap.Scope.AddExpressionBinding(outer.analyzer, fnone, liburl(), outer.newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
                }

                addClass("mmap", liburl(), mmap);
            }
        }


        class NisModule : NativeModule
        {
            public NisModule(Builtins outer)
                : base(outer, "nis")
            {
            }

            public override void initBindings()
            {
                addStrFuncs("match", "cat", "get_default_domain");
                addFunction("maps", liburl(), outer.newList(DataType.Str));
                addClass("error", liburl(), outer.newException("error", table));
            }
        }


        class OsModule : NativeModule
        {
            public OsModule(Builtins outer) : base(outer, "os") { }

            public override void initBindings()
            {
                addAttr("name", liburl(), DataType.Str);
                addClass("error", liburl(), outer.newException("error", table));  // XXX: OSError

                initProcBindings();
                initProcMgmtBindings();
                initFileBindings();
                initFileAndDirBindings();
                initMiscSystemInfo();
                initOsPathModule();

                addAttr("errno", liburl(), outer.newModule("errno"));

                addFunction("urandom", liburl("miscellaneous-functions"), DataType.Str);
                addAttr("NGROUPS_MAX", liburl(), DataType.Int);

                foreach (string s in new[] {"_Environ", "_copy_reg", "_execvpe", "_exists",
                        "_get_exports_list", "_make_stat_result",
                        "_make_statvfs_result", "_pickle_stat_result",
                        "_pickle_statvfs_result", "_spawnvef"})
                {
                    addFunction(s, liburl(), DataType.Unknown);
                }
            }


            private void initProcBindings()
            {
                string a = "process-parameters";

                addAttr("environ", liburl(a), outer.newDict(DataType.Str, DataType.Str));

                foreach (string s in new[] { "chdir", "fchdir", "putenv", "setegid", "seteuid",
                        "setgid", "setgroups", "setpgrp", "setpgid",
                        "setreuid", "setregid", "setuid", "unsetenv"})
                {
                    addFunction(s, liburl(a), DataType.None);
                }

                foreach (string s in new[] {"getegid", "getgid", "getpgid", "getpgrp",
                        "getppid", "getuid", "getsid", "umask"})
                {
                    addFunction(s, liburl(a), DataType.Int);
                }

                foreach (string s in new[] { "getcwd", "ctermid", "getlogin", "getenv", "strerror" })
                {
                    addFunction(s, liburl(a), DataType.Str);
                }

                addFunction("getgroups", liburl(a), outer.newList(DataType.Str));
                addFunction("uname", liburl(a), outer.newTuple(DataType.Str, DataType.Str, DataType.Str,
                        DataType.Str, DataType.Str));
            }


            private void initProcMgmtBindings()
            {
                string a = "process-management";

                foreach (string s in new[] { "EX_CANTCREAT", "EX_CONFIG", "EX_DATAERR",
                        "EX_IOERR", "EX_NOHOST", "EX_NOINPUT",
                        "EX_NOPERM", "EX_NOUSER", "EX_OK", "EX_OSERR",
                        "EX_OSFILE", "EX_PROTOCOL", "EX_SOFTWARE",
                        "EX_TEMPFAIL", "EX_UNAVAILABLE", "EX_USAGE",
                        "P_NOWAIT", "P_NOWAITO", "P_WAIT", "P_DETACH",
                        "P_OVERLAY", "WCONTINUED", "WCOREDUMP",
                        "WEXITSTATUS", "WIFCONTINUED", "WIFEXITED",
                        "WIFSIGNALED", "WIFSTOPPED", "WNOHANG", "WSTOPSIG",
                        "WTERMSIG", "WUNTRACED"})
                {
                    addAttr(s, liburl(a), DataType.Int);
                }

                foreach (string s in new[] {"abort", "execl", "execle", "execlp", "execlpe",
                        "execv", "execve", "execvp", "execvpe", "_exit",
                        "kill", "killpg", "plock", "startfile"})
                {
                    addFunction(s, liburl(a), DataType.None);
                }

                foreach (string s in new[] {"nice", "spawnl", "spawnle", "spawnlp", "spawnlpe",
                        "spawnv", "spawnve", "spawnvp", "spawnvpe", "system"})
                {
                    addFunction(s, liburl(a), DataType.Int);
                }

                addFunction("fork", liburl(a), outer.newUnion(outer.BaseFileInst, DataType.Int));
                addFunction("times", liburl(a), outer.newTuple(DataType.Int, DataType.Int, DataType.Int, DataType.Int, DataType.Int));

                foreach (string s in new[] { "forkpty", "wait", "waitpid" })
                {
                    addFunction(s, liburl(a), outer.newTuple(DataType.Int, DataType.Int));
                }

                foreach (string s in new[] { "wait3", "wait4" })
                {
                    addFunction(s, liburl(a), outer.newTuple(DataType.Int, DataType.Int, DataType.Int));
                }
            }


            private void initFileBindings()
            {
                string a = "file-object-creation";

                foreach (string s in new[] { "fdopen", "popen", "tmpfile" })
                {
                    addFunction(s, liburl(a), outer.BaseFileInst);
                }

                addFunction("popen2", liburl(a), outer.newTuple(outer.BaseFileInst, outer.BaseFileInst));
                addFunction("popen3", liburl(a), outer.newTuple(outer.BaseFileInst, outer.BaseFileInst, outer.BaseFileInst));
                addFunction("popen4", liburl(a), outer.newTuple(outer.BaseFileInst, outer.BaseFileInst));

                a = "file-descriptor-operations";

                addFunction("open", liburl(a), outer.BaseFileInst);

                foreach (string s in new[] {"close", "closerange", "dup2", "fchmod",
                        "fchown", "fdatasync", "fsync", "ftruncate",
                        "lseek", "tcsetpgrp", "write"})
                {
                    addFunction(s, liburl(a), DataType.None);
                }

                foreach (string s in new[] {"dup2", "fpathconf", "fstat", "fstatvfs",
                        "isatty", "tcgetpgrp"})
                {
                    addFunction(s, liburl(a), DataType.Int);
                }

                foreach (string s in new[] { "read", "ttyname" })
                {
                    addFunction(s, liburl(a), DataType.Str);
                }

                foreach (string s in new[] {"openpty", "pipe", "fstat", "fstatvfs",
                        "isatty"})
                {
                    addFunction(s, liburl(a), outer.newTuple(DataType.Int, DataType.Int));
                }

                foreach (string s in new[] {"O_APPEND", "O_CREAT", "O_DIRECT", "O_DIRECTORY",
                        "O_DSYNC", "O_EXCL", "O_LARGEFILE", "O_NDELAY",
                        "O_NOCTTY", "O_NOFOLLOW", "O_NONBLOCK", "O_RDONLY",
                        "O_RDWR", "O_RSYNC", "O_SYNC", "O_TRUNC", "O_WRONLY",
                        "SEEK_CUR", "SEEK_END", "SEEK_SET"})
                {
                    addAttr(s, liburl(a), DataType.Int);
                }
            }


            private void initFileAndDirBindings()
            {
                string a = "files-and-directories";

                foreach (string s in new[] { "F_OK", "R_OK", "W_OK", "X_OK" })
                {
                    addAttr(s, liburl(a), DataType.Int);
                }

                foreach (string s in new[] {"chflags", "chroot", "chmod", "chown", "lchflags",
                        "lchmod", "lchown", "link", "mknod", "mkdir",
                        "mkdirs", "remove", "removedirs", "rename", "renames",
                        "rmdir", "symlink", "unlink", "utime"})
                {
                    addAttr(s, liburl(a), DataType.None);
                }

                foreach (string s in new[] {"access", "lstat", "major", "minor",
                        "makedev", "pathconf", "stat_float_times"})
                {
                    addFunction(s, liburl(a), DataType.Int);
                }

                foreach (string s in new[] { "getcwdu", "readlink", "tempnam", "tmpnam" })
                {
                    addFunction(s, liburl(a), DataType.Str);
                }

                foreach (string s in new[] { "listdir" })
                {
                    addFunction(s, liburl(a), outer.newList(DataType.Str));
                }

                addFunction("mkfifo", liburl(a), outer.BaseFileInst);

                addFunction("stat", liburl(a), outer.newList(DataType.Int));  // XXX: posix.stat_result
                addFunction("statvfs", liburl(a), outer.newList(DataType.Int));  // XXX: pos.statvfs_result

                addAttr("pathconf_names", liburl(a), outer.newDict(DataType.Str, DataType.Int));
                addAttr("TMP_MAX", liburl(a), DataType.Int);

                addFunction("walk", liburl(a), outer.newList(outer.newTuple(DataType.Str, DataType.Str, DataType.Str)));
            }


            private void initMiscSystemInfo()
            {
                string a = "miscellaneous-system-information";

                addAttr("confstr_names", liburl(a), outer.newDict(DataType.Str, DataType.Int));
                addAttr("sysconf_names", liburl(a), outer.newDict(DataType.Str, DataType.Int));

                foreach (string s in new[] {"curdir", "pardir", "sep", "altsep", "extsep",
                        "pathsep", "defpath", "linesep", "devnull"})
                {
                    addAttr(s, liburl(a), DataType.Str);
                }

                foreach (string s in new[] { "getloadavg", "sysconf" })
                {
                    addFunction(s, liburl(a), DataType.Int);
                }

                addFunction("confstr", liburl(a), DataType.Str);
            }


            private void initOsPathModule()
            {
                ModuleType m = outer.newModule("path");
                NameScope ospath = m.Scope;
                ospath.Path = "os.path";  // make sure global qnames are correct

                update("path", newLibUrl("os.path.html#module-os.path"), m, BindingKind.MODULE);

                string[] str_funcs = {
                    "_resolve_link", "abspath", "basename", "commonprefix",
                    "dirname", "expanduser", "expandvars", "join",
                    "normcase", "normpath", "realpath", "relpath",
            };
                foreach (string s in str_funcs)
                {
                    ospath.AddExpressionBinding(outer.analyzer, s, newLibUrl("os.path", s), outer.newFunc(DataType.Str), BindingKind.FUNCTION).IsBuiltin = true;
                }

                string[] num_funcs = {
                    "exists", "lexists", "getatime", "getctime", "getmtime", "getsize",
                    "isabs", "isdir", "isfile", "islink", "ismount", "samefile",
                    "sameopenfile", "samestat", "supports_unicode_filenames",
            };
                foreach (string s in num_funcs)
                {
                    ospath.AddExpressionBinding(outer.analyzer, s, newLibUrl("os.path", s), outer.newFunc(DataType.Int), BindingKind.FUNCTION).IsBuiltin = true;
                }

                foreach (string s in new[] { "split", "splitdrive", "splitext", "splitunc" })
                {
                    ospath.AddExpressionBinding(outer.analyzer, s, newLibUrl("os.path", s),
                            outer.newFunc(outer.newTuple(DataType.Str, DataType.Str)), BindingKind.FUNCTION);
                }

                ospath.AddExpressionBinding(outer.analyzer, "walk", newLibUrl("os.path"), outer.newFunc(DataType.None), BindingKind.FUNCTION).IsBuiltin = true;

                string[] str_attrs = {
                    "altsep", "curdir", "devnull", "defpath", "pardir", "pathsep", "sep",
            };
                foreach (string s in str_attrs)
                {
                    ospath.AddExpressionBinding(outer.analyzer, s, newLibUrl("os.path", s), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }

                ospath.AddExpressionBinding(outer.analyzer, "os", liburl(), this.module!, BindingKind.ATTRIBUTE).IsBuiltin = true;
                ospath.AddExpressionBinding(outer.analyzer, "stat", newLibUrl("stat"),
                    // moduleTable.lookupLocal("stat").getType(),
                        outer.newModule("<stat-fixme>"), BindingKind.ATTRIBUTE);

                // XXX:  this is an re object, I think
                ospath.AddExpressionBinding(outer.analyzer, "_varprog", newLibUrl("os.path"), DataType.Unknown, BindingKind.ATTRIBUTE).IsBuiltin = true;
            }
        }


        class OperatorModule : NativeModule
        {
            public OperatorModule(Builtins outer)
                : base(outer, "operator")
            {
            }


            public override void initBindings()
            {
                // XXX:  mark __getslice__, __setslice__ and __delslice__ as deprecated.
                addNumFuncs(
                        "__abs__", "__add__", "__and__", "__concat__", "__contains__",
                        "__div__", "__doc__", "__eq__", "__floordiv__", "__ge__",
                        "__getitem__", "__getslice__", "__gt__", "__iadd__", "__iand__",
                        "__iconcat__", "__idiv__", "__ifloordiv__", "__ilshift__",
                        "__imod__", "__imul__", "__index__", "__inv__", "__invert__",
                        "__ior__", "__ipow__", "__irepeat__", "__irshift__", "__isub__",
                        "__itruediv__", "__ixor__", "__le__", "__lshift__", "__lt__",
                        "__mod__", "__mul__", "__name__", "__ne__", "__neg__", "__not__",
                        "__or__", "__package__", "__pos__", "__pow__", "__repeat__",
                        "__rshift__", "__setitem__", "__setslice__", "__sub__",
                        "__truediv__", "__xor__", "abs", "add", "and_", "concat",
                        "contains", "countOf", "div", "eq", "floordiv", "ge", "getitem",
                        "getslice", "gt", "iadd", "iand", "iconcat", "idiv", "ifloordiv",
                        "ilshift", "imod", "imul", "index", "indexOf", "inv", "invert",
                        "ior", "ipow", "irepeat", "irshift", "isCallable",
                        "isMappingType", "isNumberType", "isSequenceType", "is_",
                        "is_not", "isub", "itruediv", "ixor", "le", "lshift", "lt", "mod",
                        "mul", "ne", "neg", "not_", "or_", "pos", "pow", "repeat",
                        "rshift", "sequenceIncludes", "setitem", "setslice", "sub",
                        "truediv", "truth", "xor");

                addUnknownFuncs("attrgetter", "itemgetter", "methodcaller");
                addNoneFuncs("__delitem__", "__delslice__", "delitem", "delclice");
            }
        }


        class ParserModule : NativeModule
        {
            public ParserModule(Builtins outer)
                : base(outer, "parser")
            {
            }

            public override void initBindings()
            {
                ClassType st = outer.newClass("st", table, outer.objectType);
                st.Scope.AddExpressionBinding(outer.analyzer, "compile", newLibUrl("parser", "st-objects"),
                        outer.newFunc(), BindingKind.METHOD);
                st.Scope.AddExpressionBinding(outer.analyzer, "isexpr", newLibUrl("parser", "st-objects"),
                        outer.newFunc(DataType.Int), BindingKind.METHOD);
                st.Scope.AddExpressionBinding(outer.analyzer, "issuite", newLibUrl("parser", "st-objects"),
                        outer.newFunc(DataType.Int), BindingKind.METHOD);
                st.Scope.AddExpressionBinding(outer.analyzer, "tolist", newLibUrl("parser", "st-objects"),
                        outer.newFunc(outer.newList()), BindingKind.METHOD);
                st.Scope.AddExpressionBinding(outer.analyzer, "totuple", newLibUrl("parser", "st-objects"),
                        outer.newFunc(outer.newTuple()), BindingKind.METHOD);

                addAttr("STType", liburl("st-objects"), outer.BaseType);

                foreach (string s in new[] { "expr", "suite", "sequence2st", "tuple2st" })
                {
                    addFunction(s, liburl("creating-st-objects"), st);
                }

                addFunction("st2list", liburl("converting-st-objects"), outer.newList());
                addFunction("st2tuple", liburl("converting-st-objects"), outer.newTuple());
                addFunction("compilest", liburl("converting-st-objects"), DataType.Unknown);

                addFunction("isexpr", liburl("queries-on-st-objects"), DataType.Bool);
                addFunction("issuite", liburl("queries-on-st-objects"), DataType.Bool);

                addClass("ParserError", liburl("exceptions-and-error-handling"),
                        outer.newException("ParserError", table));
            }
        }


        class PosixModule : NativeModule
        {
            public PosixModule(Builtins outer) : base(outer, "posix") { }


            public override void initBindings()
            {
                addAttr("environ", liburl(), outer.newDict(DataType.Str, DataType.Str));
            }
        }


        class PwdModule : NativeModule
        {
            public PwdModule(Builtins outer) : base(outer, "pwd") { }


            public override void initBindings()
            {
                ClassType struct_pwd = outer.newClass("struct_pwd", table, outer.objectType);
                foreach (string s in new [] {"pw_nam", "pw_passwd", "pw_uid", "pw_gid",
                        "pw_gecos", "pw_dir", "pw_shell"})
                {
                    struct_pwd.Scope.AddExpressionBinding(outer.analyzer, s, liburl(), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }
                addAttr("struct_pwd", liburl(), struct_pwd);

                addFunction("getpwuid", liburl(), struct_pwd);
                addFunction("getpwnam", liburl(), struct_pwd);
                addFunction("getpwall", liburl(), outer.newList(struct_pwd));
            }
        }


        class PyexpatModule : NativeModule
        {
            public PyexpatModule(Builtins outer) : base(outer, "pyexpat") { }


            public override void initBindings()
            {
                // XXX
            }
        }


        class ReadlineModule : NativeModule
        {
            public ReadlineModule(Builtins outer) : base(outer, "readline") { }


            public override void initBindings()
            {
                addNoneFuncs("parse_and_bind", "insert_text", "read_init_file",
                        "read_history_file", "write_history_file",
                        "clear_history", "set_history_length",
                        "remove_history_item", "replace_history_item",
                        "redisplay", "set_startup_hook", "set_pre_input_hook",
                        "set_completer", "set_completer_delims",
                        "set_completion_display_matches_hook", "add_history");

                addNumFuncs("get_history_length", "get_current_history_length",
                        "get_begidx", "get_endidx");

                addStrFuncs("get_line_buffer", "get_history_item");

                addUnknownFuncs("get_completion_type");

                addFunction("get_completer", liburl(), outer.newFunc());
                addFunction("get_completer_delims", liburl(), outer.newList(DataType.Str));
            }
        }


        class ResourceModule : NativeModule
        {
            public ResourceModule(Builtins outer) : base(outer, "resource") { }


            public override void initBindings()
            {
                addFunction("getrlimit", liburl(), outer.newTuple(DataType.Int, DataType.Int));
                addFunction("getrlimit", liburl(), DataType.Unknown);

                string[] constants = {
                    "RLIMIT_CORE", "RLIMIT_CPU", "RLIMIT_FSIZE", "RLIMIT_DATA",
                    "RLIMIT_STACK", "RLIMIT_RSS", "RLIMIT_NPROC", "RLIMIT_NOFILE",
                    "RLIMIT_OFILE", "RLIMIT_MEMLOCK", "RLIMIT_VMEM", "RLIMIT_AS"
            };
                foreach (string c in constants)
                {
                    addAttr(c, liburl("resource-limits"), DataType.Int);
                }

                ClassType ru = outer.newClass("struct_rusage", table, outer.objectType);
                string[] ru_fields = {
                    "ru_utime", "ru_stime", "ru_maxrss", "ru_ixrss", "ru_idrss",
                    "ru_isrss", "ru_minflt", "ru_majflt", "ru_nswap", "ru_inblock",
                    "ru_oublock", "ru_msgsnd", "ru_msgrcv", "ru_nsignals",
                    "ru_nvcsw", "ru_nivcsw"
            };
                foreach (string ruf in ru_fields)
                {
                    ru.Scope.AddExpressionBinding(outer.analyzer, ruf, liburl("resource-usage"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }

                addFunction("getrusage", liburl("resource-usage"), ru);
                addFunction("getpagesize", liburl("resource-usage"), DataType.Int);

                foreach (string s in new[] { "RUSAGE_SELF", "RUSAGE_CHILDREN", "RUSAGE_BOTH" })
                {
                    addAttr(s, liburl("resource-usage"), DataType.Int);
                }
            }
        }


        class SelectModule : NativeModule
        {
            public SelectModule(Builtins outer) : base(outer, "select") { }


            public override void initBindings()
            {
                addClass("error", liburl(), outer.newException("error", table));

                addFunction("select", liburl(), outer.newTuple(outer.newList(), outer.newList(), outer.newList()));

                string a = "edge-and-level-trigger-polling-epoll-objects";

                ClassType epoll = outer.newClass("epoll", table, outer.objectType);
                epoll.Scope.AddExpressionBinding(outer.analyzer, "close", newLibUrl("select", a), outer.newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
                epoll.Scope.AddExpressionBinding(outer.analyzer, "fileno", newLibUrl("select", a), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                epoll.Scope.AddExpressionBinding(outer.analyzer, "fromfd", newLibUrl("select", a), outer.newFunc(epoll), BindingKind.METHOD).IsBuiltin = true;
                foreach (string s in new[] { "register", "modify", "unregister", "poll" })
                {
                    epoll.Scope.AddExpressionBinding(outer.analyzer, s, newLibUrl("select", a), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                }
                addClass("epoll", liburl(a), epoll);

                foreach (string s in new [] {"EPOLLERR", "EPOLLET", "EPOLLHUP", "EPOLLIN", "EPOLLMSG",
                        "EPOLLONESHOT", "EPOLLOUT", "EPOLLPRI", "EPOLLRDBAND",
                        "EPOLLRDNORM", "EPOLLWRBAND", "EPOLLWRNORM"})
                {
                    addAttr(s, liburl(a), DataType.Int);
                }

                a = "polling-objects";

                ClassType poll = outer.newClass("poll", table, outer.objectType);
                poll.Scope.AddExpressionBinding(outer.analyzer, "register", newLibUrl("select", a), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                poll.Scope.AddExpressionBinding(outer.analyzer, "modify",    newLibUrl("select", a), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                poll.Scope.AddExpressionBinding(outer.analyzer, "unregister", newLibUrl("select", a), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                poll.Scope.AddExpressionBinding(outer.analyzer, "poll", newLibUrl("select", a),
                        outer.newFunc(outer.newList(outer.newTuple(DataType.Int, DataType.Int))), BindingKind.METHOD);
                addClass("poll", liburl(a), poll);

                foreach (string s in new[] {"POLLERR", "POLLHUP", "POLLIN", "POLLMSG",
                        "POLLNVAL", "POLLOUT", "POLLPRI", "POLLRDBAND",
                        "POLLRDNORM", "POLLWRBAND", "POLLWRNORM"})
                {
                    addAttr(s, liburl(a), DataType.Int);
                }

                a = "kqueue-objects";

                ClassType kqueue = outer.newClass("kqueue", table, outer.objectType);
                kqueue.Scope.AddExpressionBinding(outer.analyzer, "close", newLibUrl("select", a), outer.newFunc(DataType.None), BindingKind.METHOD).IsBuiltin = true;
                kqueue.Scope.AddExpressionBinding(outer.analyzer, "fileno", newLibUrl("select", a), outer.newFunc(DataType.Int), BindingKind.METHOD).IsBuiltin = true;
                kqueue.Scope.AddExpressionBinding(outer.analyzer, "fromfd", newLibUrl("select", a), outer.newFunc(kqueue), BindingKind.METHOD).IsBuiltin = true;
                kqueue.Scope.AddExpressionBinding(outer.analyzer, "control", newLibUrl("select", a),
                        outer.newFunc(outer.newList(outer.newTuple(DataType.Int, DataType.Int))), BindingKind.METHOD);
                addClass("kqueue", liburl(a), kqueue);

                a = "kevent-objects";

                ClassType kevent = outer.newClass("kevent", table, outer.objectType);
                foreach (string s in new[] { "ident", "filter", "flags", "fflags", "data", "udata" })
                {
                    kevent.Scope.AddExpressionBinding(outer.analyzer, s, newLibUrl("select", a), DataType.Unknown, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }
                addClass("kevent", liburl(a), kevent);
            }
        }


        class SignalModule : NativeModule
        {
            public SignalModule(Builtins outer) : base(outer, "signal") { }

            public override void initBindings()
            {
                addNumAttrs(
                        "NSIG", "SIGABRT", "SIGALRM", "SIGBUS", "SIGCHLD", "SIGCLD",
                        "SIGCONT", "SIGFPE", "SIGHUP", "SIGILL", "SIGINT", "SIGIO",
                        "SIGIOT", "SIGKILL", "SIGPIPE", "SIGPOLL", "SIGPROF", "SIGPWR",
                        "SIGQUIT", "SIGRTMAX", "SIGRTMIN", "SIGSEGV", "SIGSTOP", "SIGSYS",
                        "SIGTERM", "SIGTRAP", "SIGTSTP", "SIGTTIN", "SIGTTOU", "SIGURG",
                        "SIGUSR1", "SIGUSR2", "SIGVTALRM", "SIGWINCH", "SIGXCPU", "SIGXFSZ",
                        "SIG_DFL", "SIG_IGN");

                addUnknownFuncs("default_int_handler", "getsignal", "set_wakeup_fd", "signal");
            }
        }


        class ShaModule : NativeModule
        {
            public ShaModule(Builtins outer) : base(outer, "sha") { }


            public override void initBindings()
            {
                addNumAttrs("blocksize", "digest_size");

                ClassType sha = outer.newClass("sha", table, outer.objectType);
                sha.Scope.AddExpressionBinding(outer.analyzer, "update", liburl(), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                sha.Scope.AddExpressionBinding(outer.analyzer, "digest", liburl(), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                sha.Scope.AddExpressionBinding(outer.analyzer, "hexdigest", liburl(), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                sha.Scope.AddExpressionBinding(outer.analyzer, "copy", liburl(), outer.newFunc(sha), BindingKind.METHOD).IsBuiltin = true;
                addClass("sha", liburl(), sha);

                update("new", liburl(), outer.newFunc(sha), BindingKind.CONSTRUCTOR);
            }
        }


        class SpwdModule : NativeModule
        {
            public SpwdModule(Builtins outer) : base(outer, "spwd") { }


            public override void initBindings()
            {
                ClassType struct_spwd = outer.newClass("struct_spwd", table, outer.objectType);
                foreach (string s in new[]{ "sp_nam", "sp_pwd", "sp_lstchg", "sp_min",
                        "sp_max", "sp_warn", "sp_inact", "sp_expire",
                        "sp_flag"})
                {
                    struct_spwd.Scope.AddExpressionBinding(outer.analyzer, s, liburl(), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }
                addAttr("struct_spwd", liburl(), struct_spwd);

                addFunction("getspnam", liburl(), struct_spwd);
                addFunction("getspall", liburl(), outer.newList(struct_spwd));
            }
        }


        class StropModule : NativeModule
        {
            public StropModule(Builtins outer) : base(outer, "strop") { }

            public override void initBindings()
            {
                table!.AddAllBindings(DataType.Str.Scope);
            }
        }


        class StructModule : NativeModule
        {
            public StructModule(Builtins outer) : base(outer, "struct") { }

            public override void initBindings()
            {
                addClass("error", liburl(), outer.newException("error", table));
                addStrFuncs("pack");
                addUnknownFuncs("pack_into");
                addNumFuncs("calcsize");
                addFunction("unpack", liburl(), outer.newTuple());
                addFunction("unpack_from", liburl(), outer.newTuple());

                outer.BaseStruct = outer.newClass("Struct", table, outer.objectType);
                addClass("Struct", liburl("struct-objects"), outer.BaseStruct);
                NameScope t = outer.BaseStruct.Scope;
                t.AddExpressionBinding(outer.analyzer, "pack", liburl("struct-objects"), outer.newFunc(DataType.Str), BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "pack_into", liburl("struct-objects"), outer.newFunc(), BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "unpack", liburl("struct-objects"), outer.newFunc(outer.newTuple()), BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "unpack_from", liburl("struct-objects"), outer.newFunc(outer.newTuple()), BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "format", liburl("struct-objects"), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "size", liburl("struct-objects"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
            }
        }


        class SysModule : NativeModule
        {
            public SysModule(Builtins outer) : base(outer, "sys") { }


            public override void initBindings()
            {
                addUnknownFuncs(
                        "_clear_type_cache", "call_tracing", "callstats", "_current_frames",
                        "_getframe", "displayhook", "dont_write_bytecode", "exitfunc",
                        "exc_clear", "exc_info", "excepthook", "exit",
                        "last_traceback", "last_type", "last_value", "modules",
                        "path_hooks", "path_importer_cache", "getprofile", "gettrace",
                        "setcheckinterval", "setprofile", "setrecursionlimit", "settrace");

                addAttr("exc_type", liburl(), DataType.None);

                addUnknownAttrs("__stderr__", "__stdin__", "__stdout__",
                        "stderr", "stdin", "stdout", "version_info");

                addNumAttrs("api_version", "hexversion", "winver", "maxint", "maxsize",
                        "maxunicode", "py3kwarning", "dllhandle");

                addStrAttrs("platform", "byteorder", "copyright", "prefix", "version",
                        "exec_prefix", "executable");

                addNumFuncs("getrecursionlimit", "getwindowsversion", "getrefcount",
                        "getsizeof", "getcheckinterval");

                addStrFuncs("getdefaultencoding", "getfilesystemencoding");

                foreach (string s in new [] {"argv", "builtin_module_names", "path",
                        "meta_path", "subversion"} )
                {
                    addAttr(s, liburl(), outer.newList(DataType.Str));
                }

                foreach (string s in new[] { "flags", "warnoptions", "float_info" })
                {
                    addAttr(s, liburl(), outer.newDict(DataType.Str, DataType.Int));
                }
            }
        }


        class SyslogModule : NativeModule
        {
            public SyslogModule(Builtins outer) : base(outer, "syslog") { }


            public override void initBindings()
            {
                addNoneFuncs("syslog", "openlog", "closelog", "setlogmask");
                addNumAttrs("LOG_ALERT", "LOG_AUTH", "LOG_CONS", "LOG_CRIT", "LOG_CRON",
                        "LOG_DAEMON", "LOG_DEBUG", "LOG_EMERG", "LOG_ERR", "LOG_INFO",
                        "LOG_KERN", "LOG_LOCAL0", "LOG_LOCAL1", "LOG_LOCAL2", "LOG_LOCAL3",
                        "LOG_LOCAL4", "LOG_LOCAL5", "LOG_LOCAL6", "LOG_LOCAL7", "LOG_LPR",
                        "LOG_MAIL", "LOG_MASK", "LOG_NDELAY", "LOG_NEWS", "LOG_NOTICE",
                        "LOG_NOWAIT", "LOG_PERROR", "LOG_PID", "LOG_SYSLOG", "LOG_UPTO",
                        "LOG_USER", "LOG_UUCP", "LOG_WARNING");
            }
        }


        class TermiosModule : NativeModule
        {
            public TermiosModule(Builtins outer) : base(outer, "termios") { }


            public override void initBindings()
            {
                addFunction("tcgetattr", liburl(), outer.newList());
                addUnknownFuncs("tcsetattr", "tcsendbreak", "tcdrain", "tcflush", "tcflow");
            }
        }


        class ThreadModule : NativeModule
        {
            public ThreadModule(Builtins outer) : base(outer, "thread") { }

            public override void initBindings()
            {
                addClass("error", liburl(), outer.newException("error", table));

                ClassType @lock = outer.newClass("lock", table, outer.objectType);
                @lock.Scope.AddExpressionBinding(outer.analyzer, "acquire", liburl(), DataType.Int, BindingKind.METHOD).IsBuiltin = true;
                @lock.Scope.AddExpressionBinding(outer.analyzer, "locked", liburl(), DataType.Int, BindingKind.METHOD).IsBuiltin = true;
                @lock.Scope.AddExpressionBinding(outer.analyzer, "release", liburl(), DataType.None, BindingKind.METHOD).IsBuiltin = true;
                addAttr("LockType", liburl(), outer.BaseType);

                addNoneFuncs("interrupt_main", "exit", "exit_thread");
                addNumFuncs("start_new", "start_new_thread", "get_ident", "stack_size");

                addFunction("allocate", liburl(), @lock);
                addFunction("allocate_lock", liburl(), @lock);  // synonym

                addAttr("_local", liburl(), outer.BaseType);
            }
        }

        class TimeModule : NativeModule
        {
            public TimeModule(Builtins outer) : base(outer, "time") { }

            public override void initBindings()
            {
                InstanceType struct_time = outer.Time_struct_time = new InstanceType(outer.newClass("datetime", table, outer.objectType));
                addAttr("struct_time", liburl(), struct_time);

                string[] struct_time_attrs = {
                    "n_fields", "n_sequence_fields", "n_unnamed_fields",
                    "tm_hour", "tm_isdst", "tm_mday", "tm_min",
                    "tm_mon", "tm_wday", "tm_yday", "tm_year",
            };
                foreach (string s in struct_time_attrs)
                {
                    struct_time.Scope.AddExpressionBinding(outer.analyzer, s, liburl("struct_time"), DataType.Int, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }

                addNumAttrs("accept2dyear", "altzone", "daylight", "timezone");

                addAttr("tzname", liburl(), outer.newTuple(DataType.Str, DataType.Str));
                addNoneFuncs("sleep", "tzset");

                addNumFuncs("clock", "mktime", "time", "tzname");
                addStrFuncs("asctime", "ctime", "strftime");

                addFunctions_beCareful(struct_time, "gmtime", "localtime", "strptime");
            }
        }

        class UnicodedataModule : NativeModule
        {
            public UnicodedataModule(Builtins outer) : base(outer, "unicodedata") { }

            public override void initBindings()
            {
                addNumFuncs("decimal", "digit", "numeric", "combining",
                        "east_asian_width", "mirrored");
                addStrFuncs("lookup", "name", "category", "bidirectional",
                        "decomposition", "normalize");
                addNumAttrs("unidata_version");
                addUnknownAttrs("ucd_3_2_0");
            }
        }

        class ZipimportModule : NativeModule
        {
            public ZipimportModule(Builtins outer) : base(outer, "zipimport") { }

            public override void initBindings()
            {
                addClass("ZipImportError", liburl(), outer.newException("ZipImportError", table));

                ClassType zipimporter = outer.newClass("zipimporter", table, outer.objectType);
                NameScope t = zipimporter.Scope;
                t.AddExpressionBinding(outer.analyzer, "find_module", liburl(), zipimporter, BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "get_code", liburl(), DataType.Unknown, BindingKind.METHOD).IsBuiltin = true;  // XXX:  code object
                t.AddExpressionBinding(outer.analyzer, "get_data", liburl(), DataType.Unknown, BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "get_source", liburl(), DataType.Str, BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "is_package", liburl(), DataType.Int, BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "load_module", liburl(), outer.newModule("<?>"), BindingKind.METHOD).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "archive", liburl(), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
                t.AddExpressionBinding(outer.analyzer, "prefix", liburl(), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;

                addClass("zipimporter", liburl(), zipimporter);
                addAttr("_zip_directory_cache", liburl(), outer.newDict(DataType.Str, DataType.Unknown));
            }
        }


        class ZlibModule : NativeModule
        {
            public ZlibModule(Builtins outer) : base(outer, "zlib") { }


            public override void initBindings()
            {
                ClassType compress = outer.newClass("Compress", table, outer.objectType);
                foreach (string s in new[] { "compress", "flush" })
                {
                    compress.Scope.AddExpressionBinding(outer.analyzer, s, newLibUrl("zlib"), DataType.Str, BindingKind.METHOD).IsBuiltin = true;
                }
                compress.Scope.AddExpressionBinding(outer.analyzer, "copy", newLibUrl("zlib"), compress, BindingKind.METHOD).IsBuiltin = true;
                addClass("Compress", liburl(), compress);

                ClassType decompress = outer.newClass("Decompress", table, outer.objectType);
                foreach (string s in new[] { "unused_data", "unconsumed_tail" })
                {
                    decompress.Scope.AddExpressionBinding(outer.analyzer, s, newLibUrl("zlib"), DataType.Str, BindingKind.ATTRIBUTE).IsBuiltin = true;
                }
                foreach (string s in new[] { "decompress", "flush" })
                {
                    decompress.Scope.AddExpressionBinding(outer.analyzer, s, newLibUrl("zlib"), DataType.Str, BindingKind.METHOD).IsBuiltin = true;
                }
                decompress.Scope.AddExpressionBinding(outer.analyzer, "copy", newLibUrl("zlib"), decompress, BindingKind.METHOD).IsBuiltin = true;
                addClass("Decompress", liburl(), decompress);

                addFunction("adler32", liburl(), DataType.Int);
                addFunction("compress", liburl(), DataType.Str);
                addFunction("compressobj", liburl(), compress);
                addFunction("crc32", liburl(), DataType.Int);
                addFunction("decompress", liburl(), DataType.Str);
                addFunction("decompressobj", liburl(), decompress);
            }
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}