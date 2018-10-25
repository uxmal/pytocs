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

using Pytocs.CodeModel;
using Pytocs.Syntax;
using Pytocs.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    /// <summary>
    /// Translates Python type references to CodeModel
    /// type references.
    /// </summary>
    public class TypeReferenceTranslator
    {
        private const string SystemNamespace = "System";
        private const string GenericCollectionNamespace = "System.Collections.Generic";

        private Dictionary<Node, DataType> types;

        public TypeReferenceTranslator(Dictionary<Node, DataType> types)
        {
            this.types = types;
        }

        public DataType TypeOf(Node node)
        {
            if (!types.TryGetValue(node, out var result))
            {
                result = DataType.Unknown;
            }
            return result;
        }

        public (CodeTypeReference, ISet<string>) TranslateTypeOf(Node node)
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

        public (CodeTypeReference, ISet<string>) Translate(DataType dt)
        {
            switch (dt)
            {
            case DictType dict:
                var (dtKey, nmKey) = Translate(dict.KeyType);
                var (dtValue, nmValue) = Translate(dict.ValueType);
                var nms = Join(nmKey, nmValue);
                return (
                    new CodeTypeReference("Dictionary", dtKey, dtValue),
                    Join(nms, GenericCollectionNamespace));
            case InstanceType inst:
                return (new CodeTypeReference(typeof(object)), null);
            case ListType list:
                var (dtElem, nmElem) = Translate(list.eltType);
                return (
                    new CodeTypeReference("List", dtElem),
                    Join(nmElem, GenericCollectionNamespace));
            case StrType str:
                return (
                    new CodeTypeReference(typeof(string)),
                    null);
            case IntType _:
                return (
                    new CodeTypeReference(typeof(int)),
                    null);
            case FloatType _:
                return (
                    new CodeTypeReference(typeof(float)),
                    null);
            case BoolType _:
                return (
                    new CodeTypeReference(typeof(bool)),
                    null);
            case TupleType tuple:
                return TranslateTuple(tuple);
            }
            throw new NotImplementedException($"Data type {dt} ({dt.GetType().Name}).");
        }

        private (CodeTypeReference, ISet<string>) TranslateTuple(TupleType tuple)
        {
            ISet<string> namespaces = null;
            var elementTypes = new List<CodeTypeReference>();
            foreach (var type in tuple.eltTypes)
            {
                var (et, nm) = Translate(type);
                elementTypes.Add(et);
                namespaces = Join(namespaces, nm);
            }
            var tt = new CodeTypeReference(
                "Tuple",
                elementTypes.ToArray());
            return (tt, Join(namespaces, SystemNamespace));
        }

        private ISet<string> Join(ISet<string> a, ISet<string> b)
        {
            if (a == null && b == null)
                return null;
            if (a == null)
                return b;
            if (b == null)
                return a;
            var result = new HashSet<string>(a);
            result.UnionWith(b);
            return result;
        }

        private ISet<string> Join(ISet<string> a, string b)
        {
            if (b == null)
                return a;
            if (a == null)
                return new HashSet<string> { b };
            a.Add(b);
            return a;
        }

        public (CodeTypeReference, ISet<string>) TranslateListElementType(Exp l)
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
    }
}
