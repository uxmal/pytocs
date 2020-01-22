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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.Translate
{
    /// <summary>
    ///     Translates Python type references to CodeModel
    ///     type references.
    /// </summary>
    public class TypeReferenceTranslator
    {
        public const string SystemNamespace = "System";
        public const string GenericCollectionNamespace = "System.Collections.Generic";
        public const string TasksNamespace = "System.Threading.Tasks";

        private static readonly Dictionary<string, (string, string)> DefaultClassTranslations =
            new Dictionary<string, (string, string)>
            {
                {"None", ("void", null)},
                {"NotImplementedError", (nameof(NotImplementedException), SystemNamespace)}
            };

        private readonly Dictionary<string, (string, string)> pyToCsClasses;
        private readonly Stack<DataType> stackq;

        private readonly Dictionary<Node, DataType> types;

        public TypeReferenceTranslator(Dictionary<Node, DataType> types,
            Dictionary<string, (string, string)> pyToCsClasses = null)
        {
            this.types = types;
            stackq = new Stack<DataType>();
            this.pyToCsClasses = pyToCsClasses ?? DefaultClassTranslations;
        }

        public virtual DataType TypeOf(Node node)
        {
            if (!types.TryGetValue(node, out DataType result))
            {
                result = DataType.Unknown;
            }

            return result;
        }

        public (CodeTypeReference, ISet<string>) TranslateTypeOf(Node node)
        {
            if (types.TryGetValue(node, out DataType dt))
            {
                return Translate(dt);
            }

            return (new CodeTypeReference(typeof(object)), null);
        }

        /// <summary>
        ///     Given a Python <see cref="DataType" />, translates it to its C# equivalent
        ///     and wraps it in a <see cref="CodeTypeReference" />
        /// </summary>
        /// <param name="dt">Data type reference to translate.</param>
        /// <returns>
        ///     A tuple of the resulting <see cref="CodeTypeReference" /> and a set
        ///     of any namespaces that should be imported.
        /// </returns>
        public (CodeTypeReference csTypeRef, ISet<string> namespaces) Translate(DataType dt)
        {
            if (stackq.Contains(dt))
            {
                return (new CodeTypeReference(typeof(object)), null);
            }

            switch (dt)
            {
                case DictType dict:
                    stackq.Push(dt);
                    (CodeTypeReference dtKey, ISet<string> nmKey) = Translate(dict.KeyType);
                    (CodeTypeReference dtValue, ISet<string> nmValue) = Translate(dict.ValueType);
                    ISet<string> nms = Join(nmKey, nmValue);
                    stackq.Pop();
                    return (
                        new CodeTypeReference("Dictionary", dtKey, dtValue),
                        Join(nms, GenericCollectionNamespace));

                case InstanceType inst:
                    return TranslateInstance(inst);

                case ListType list:
                    stackq.Push(dt);
                    (CodeTypeReference dtElem, ISet<string> nmElem) = Translate(list.eltType);
                    stackq.Pop();
                    return (
                        new CodeTypeReference("List", dtElem),
                        Join(nmElem, GenericCollectionNamespace));

                case SetType set:
                    stackq.Push(dt);
                    (CodeTypeReference dtSetElem, ISet<string> nmSetElem) = Translate(set.ElementType);
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
                        new HashSet<string> { "System.Numeric" });

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
                    (CodeTypeReference, ISet<string>) ft = TranslateFunc(fun);
                    stackq.Pop();
                    return ft;

                case ClassType classType:
                    return TranslateClass(classType);

                case ModuleType module:
                    return (
                        new CodeTypeReference(module.name),
                        null);
            }

            throw new NotImplementedException($"Data type {dt} ({dt.GetType().Name}).");
        }

        private (CodeTypeReference csTypeRef, ISet<string> namespaces) TranslateClass(ClassType classType)
        {
            if (!pyToCsClasses.TryGetValue(classType.name, out (string, string) nameAndSpace))
            {
                nameAndSpace = (classType.name, null);
            }

            CodeTypeReference csClass = new CodeTypeReference(nameAndSpace.Item1);
            HashSet<string> nmset = nameAndSpace.Item2 != null
                ? new HashSet<string> { nameAndSpace.Item2 }
                : null;
            return (csClass, nmset);
        }

        private (CodeTypeReference, ISet<string>) TranslateInstance(InstanceType inst)
        {
            if (inst == DataType.Unknown)
            {
                return (new CodeTypeReference(typeof(object)
                ), null);
            }

            return Translate(inst.classType);
        }

        private (CodeTypeReference, ISet<string>) TranslateFunc(FunType fun)
        {
            if (fun.arrows.Count != 0)
            {
                // Pick an arrow at random.
                KeyValuePair<DataType, DataType> arrow = fun.arrows.First();
                (CodeTypeReference args, ISet<string> nms) = Translate(arrow.Key);
                if (arrow.Value is InstanceType i && i.classType is ClassType c && c.name == "None")
                {
                    return (
                        new CodeTypeReference("Action", args),
                        Join(nms, SystemNamespace));
                }

                (CodeTypeReference ret, ISet<string> nmsRet) = Translate(arrow.Value);
                return (
                    new CodeTypeReference("Func", args),
                    Join(nms, Join(nmsRet, SystemNamespace)));
            }

            return (
                new CodeTypeReference("Func",
                    new CodeTypeReference(typeof(object)),
                    new CodeTypeReference(typeof(object))),
                Join(null, SystemNamespace));
        }

        private (CodeTypeReference, ISet<string>) TranslateTuple(TupleType tuple)
        {
            ISet<string> namespaces = null;
            List<DataType> types = tuple.eltTypes;
            (List<CodeTypeReference> elementTypes, ISet<string> nms) = TranslateTypes(types, namespaces);
            CodeTypeReference tt = new CodeTypeReference(
                "Tuple",
                elementTypes.ToArray());
            return (tt, Join(nms, SystemNamespace));
        }

        private (List<CodeTypeReference>, ISet<string>) TranslateTypes(IEnumerable<DataType> types,
            ISet<string> namespaces)
        {
            List<CodeTypeReference> elementTypes = new List<CodeTypeReference>();
            foreach (DataType type in types)
            {
                (CodeTypeReference et, ISet<string> nm) = Translate(type);
                elementTypes.Add(et);
                namespaces = Join(namespaces, nm);
            }

            return (elementTypes, namespaces);
        }

        private ISet<string> Join(ISet<string> a, ISet<string> b)
        {
            if (a == null && b == null)
            {
                return null;
            }

            if (a == null)
            {
                return b;
            }

            if (b == null)
            {
                return a;
            }

            HashSet<string> result = new HashSet<string>(a);
            result.UnionWith(b);
            return result;
        }

        private ISet<string> Join(ISet<string> a, string b)
        {
            if (b == null)
            {
                return a;
            }

            if (a == null)
            {
                return new HashSet<string> { b };
            }

            a.Add(b);
            return a;
        }

        public (CodeTypeReference, ISet<string>) TranslateListElementType(Exp l)
        {
            DataType dt = TypeOf(l);
            if (dt is ListType listType)
            {
                return Translate(listType.eltType);
            }

            return (
                new CodeTypeReference(typeof(object)),
                null);
        }
    }
}