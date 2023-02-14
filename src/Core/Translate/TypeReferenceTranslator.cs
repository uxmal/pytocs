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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate
{
    /// <summary>
    /// Translates Python type references to CodeModel
    /// type references.
    /// </summary>
    public class TypeReferenceTranslator
    {
        public const string SystemNamespace = "System";
        public const string GenericCollectionNamespace = "System.Collections.Generic";
        public const string LinqNamespace = "System.Linq";
        public const string NumericNamespace = "System.Numeric";
        public const string TasksNamespace = "System.Threading.Tasks";

        private readonly Dictionary<Node, DataType> types;
        private readonly Stack<DataType> stackq;
        private readonly Dictionary<string, (string, string?)> pyToCsClasses;

        public TypeReferenceTranslator(Dictionary<Node, DataType> types, Dictionary<string, (string,string?)>? pyToCsClasses = null)
        {
            this.types = types;
            this.stackq = new Stack<DataType>();
            this.pyToCsClasses = pyToCsClasses ?? DefaultClassTranslations;
        }

        public virtual DataType TypeOf(Node node)
        {
            if (!types.TryGetValue(node, out var result))
            {
                result = DataType.Unknown;
            }
            return result;
        }

        public (CodeTypeReference, ISet<string>?) TranslateTypeOf(Node node)
        {
            if (types.TryGetValue(node, out var dt))
            {
                return Translate(dt);
            }
            else
            {
                return (new CodeTypeReference(typeof(object)), null);
            }
        }

        /// <summary>
        /// Given a Python <see cref="DataType"/>, translates it to its C# equivalent 
        /// and wraps it in a <see cref="CodeTypeReference"/>
        /// </summary>
        /// <param name="dt">Data type reference to translate.</param>
        /// <returns>A tuple of the resulting <see cref="CodeTypeReference"/> and a set 
        /// of any namespaces that should be imported.
        /// </returns>
        public (CodeTypeReference csTypeRef, ISet<string>? namespaces) Translate(DataType dt)
        {
            if (stackq.Contains(dt))
                return (new CodeTypeReference(typeof(object)), null);
            switch (dt)
            {
            case DictType dict:
                stackq.Push(dt); 
                var (dtKey, nmKey) = Translate(dict.KeyType);
                var (dtValue, nmValue) = Translate(dict.ValueType);
                var nms = Join(nmKey, nmValue);
                stackq.Pop();
                return (
                    new CodeTypeReference("Dictionary", dtKey, dtValue),
                    Join(nms, GenericCollectionNamespace));
            case InstanceType inst:
                return TranslateInstance(inst);
            case ListType list:
                stackq.Push(dt);
                var (dtElem, nmElem) = Translate(list.eltType);
                stackq.Pop();
                return (
                    new CodeTypeReference("List", dtElem),
                    Join(nmElem, GenericCollectionNamespace));
            case SetType set:
                stackq.Push(dt);
                var (dtSetElem, nmSetElem) = Translate(set.ElementType);
                stackq.Pop();
                return (
                    new CodeTypeReference("Set", dtSetElem),
                    Join(nmSetElem, GenericCollectionNamespace));
            case UnionType _:
                return (
                    new CodeTypeReference(typeof(object)),
                    null);
            case ComplexType _:
                return (
                    new CodeTypeReference("Complex"),
                    new HashSet<string> { NumericNamespace });
            case StrType _:
                return (
                    new CodeTypeReference(typeof(string)),
                    null);
            case IntType _:
                return (
                    new CodeTypeReference(typeof(int)),
                    null);
            case FloatType _:
                return (
                    new CodeTypeReference(typeof(double)),
                    null);
            case BoolType _:
                return (
                    new CodeTypeReference(typeof(bool)),
                    null);
            case TupleType tuple:
                return TranslateTuple(tuple);
            case FunType fun:
                stackq.Push(dt);
                var ft = TranslateFunc(fun);
                stackq.Pop();
                return ft;
            case ClassType classType:
                return TranslateClass(classType);
            case ModuleType module:
                return (
                    new CodeTypeReference(module.Name),
                    null);
            }
            throw new NotImplementedException($"Data type {dt} ({dt.GetType().Name}).");
        }

        private (CodeTypeReference csTypeRef, ISet<string>? namespaces) TranslateClass(ClassType classType)
        {
            if (!this.pyToCsClasses.TryGetValue(classType.name, out var nameAndSpace))
            {
                nameAndSpace = (classType.name, null);
            }
            var csClass = new CodeTypeReference(nameAndSpace.Item1);
            var nmset = nameAndSpace.Item2 != null
                ? new HashSet<string> { nameAndSpace.Item2 }
                : null;
            return (csClass, nmset);
        }

        private (CodeTypeReference, ISet<string>?) TranslateInstance(InstanceType inst)
        {
            if (inst == DataType.Unknown)
                return (new CodeTypeReference(typeof(object)
                    ), null);
            return this.Translate(inst.classType);
        }

        private (CodeTypeReference, ISet<string>?) TranslateFunc(FunType fun)
        {
            if (fun.arrows.Count != 0)
            {
                // Pick an arrow at random.
                var arrow = fun.arrows.First();
                var (args, nms) = Translate(arrow.Key);
                if (arrow.Value is InstanceType i && i.classType is ClassType c && c.name == "None")
                {
                    return (
                        new CodeTypeReference("Action", args),
                        Join(nms, SystemNamespace));
                }
                else
                {
                    var (ret, nmsRet) = Translate(arrow.Value);
                    return (
                        new CodeTypeReference("Func", args),
                        Join(nms, Join(nmsRet, SystemNamespace)));
                }
            }
            else
            {
                return (
                    new CodeTypeReference("Func",
                        new CodeTypeReference(typeof(object)),
                        new CodeTypeReference(typeof(object))),
                    Join(null, SystemNamespace));
            }
        }

        private (CodeTypeReference, ISet<string>?) TranslateTuple(TupleType tuple)
        {
            ISet<string>? namespaces = null;
            var types = tuple.eltTypes;
            var (elementTypes, nms) = TranslateTypes(types, namespaces);
            if (tuple.IsVariant)
            {
                // C# has no variant tuple concept, so we are forced to make this an array.
                //$TODO: find the most general unifier of elementTypes.
                var mgu = typeof(object);
                var tt = new CodeTypeReference(new CodeTypeReference(mgu), 1);
                return (tt, nms);
            }
            else
            {
                var tt = new CodeTypeReference(
                    nameof(Tuple),
                    elementTypes.ToArray());
                return (tt, Join(nms, SystemNamespace));
            }
        }

        private (List<CodeTypeReference>, ISet<string>?) TranslateTypes(IEnumerable<DataType> types, ISet<string>? namespaces)
        {
            var elementTypes = new List<CodeTypeReference>();
            foreach (var type in types)
            {
                var (et, nm) = Translate(type);
                elementTypes.Add(et);
                namespaces = Join(namespaces, nm);
            }

            return (elementTypes, namespaces);
        }

        private static ISet<string>? Join(ISet<string>? a, ISet<string>? b)
        {
            if (a is null && b is null)
                return null;
            if (a is null)
                return b;
            if (b is null)
                return a;
            var result = new HashSet<string>(a);
            result.UnionWith(b);
            return result;
        }

        private static ISet<string>? Join(ISet<string>? a, string b)
        {
            if (b is null)
                return a;
            if (a is null)
                return new HashSet<string> { b };
            a.Add(b);
            return a;
        }

        public (CodeTypeReference, ISet<string>?) TranslateListElementType(Exp l)
        {
            var dt = TypeOf(l);
            if (dt is ListType listType)
            {
                return Translate(listType.eltType);
            }
            else
            {
                return (
                    new CodeTypeReference(typeof(object)),
                    null);
            }
        }

        private static readonly Dictionary<string, (string, string?)> DefaultClassTranslations = new Dictionary<string, (string, string?)>
        {
            { "None", ("object", null) },
            { "Unit", ("void", null) },
            { "NotImplementedError", (nameof(NotImplementedException), SystemNamespace) }
        };
    }
}
