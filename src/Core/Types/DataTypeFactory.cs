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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.TypeInference;

namespace Pytocs.Core.Types
{
    public class DataTypeFactory
    {
        private AnalyzerImpl analyzer;

        public DataTypeFactory(AnalyzerImpl analyzer)
        {
            this.analyzer = analyzer;
        }

        public DictType CreateDict(DataType key, DataType value)
        {
            return Register(new DictType(key, value));
        }


        public IterableType CreateIterable(DataType it)
        {
            return Register(new IterableType(it));
        }

        public ListType CreateList()
        {
            return Register(new ListType());
        }

        public ListType CreateList(DataType elt0)
        {
            return Register(new ListType(elt0));
        }

        public ModuleType CreateModule(string name, string file, string qName, NameScope parent)
        {
            return Register(new ModuleType(name, file, qName, parent));
        }

        private ModuleType Register(ModuleType module)
        {
            // null during bootstrapping of built-in types
            if (analyzer.Builtins != null)
            {
                module.Table.AddSuper(analyzer.Builtins.BaseModule.Table);
            }
            return module;
        }

        public TupleType CreateTuple(DataType[] dataTypes)
        {
            return Register(new TupleType(dataTypes));
        }

        public TupleType CreateTuple(DataType item0)
        {
            return Register(new TupleType(item0));
        }

        private TupleType Register(TupleType tuple)
        {
            tuple.Table.AddSuper(analyzer.Builtins.BaseTuple.Table);
            tuple.Table.Path = analyzer.Builtins.BaseTuple.Table.Path;
            return tuple;
        }

        private ListType Register(ListType list)
        {
            list.Table.AddSuper(analyzer.Builtins.BaseList.Table);
            list.Table.Path = analyzer.Builtins.BaseList.Table.Path;
            return list;
        }

        private DictType Register(DictType dictType)
        {
            dictType.Table.AddSuper(analyzer.Builtins.BaseDict.Table);
            dictType.Table.Path = analyzer.Builtins.BaseDict.Table.Path;
            return dictType;
        }

        private IterableType Register(IterableType iterableType)
        {
            iterableType.Table.AddSuper(analyzer.Builtins.BaseIterable.Table);
            iterableType.Table.Path = analyzer.Builtins.BaseIterable.Table.Path;
            return iterableType;
        }
    }
}
