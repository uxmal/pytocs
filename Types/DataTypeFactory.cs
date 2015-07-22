using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.TypeInference;

namespace Pytocs.Types
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

        public ListType CreateList()
        {
            return Register(new ListType());
        }

        public ListType CreateList(DataType elt0)
        {
            return Register(new ListType(elt0));
        }

        public ModuleType CreateModule(string name, string file, string qName, State parent)
        {
            return Register(new ModuleType(name, file, qName, parent));
        }

        private ModuleType Register(ModuleType module)
        {
            // null during bootstrapping of built-in types
            if (analyzer.builtins != null)
            {
                module.Table.addSuper(analyzer.builtins.BaseModule.Table);
            }
            return module;
        }

        public TupleType CreateTuple()
        {
            return Register(new TupleType());
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
            tuple.Table.addSuper(analyzer.builtins.BaseTuple.Table);
            tuple.Table.Path = analyzer.builtins.BaseTuple.Table.Path;
            return tuple;
        }

        private ListType Register(ListType list)
        {
            list.Table.addSuper(analyzer.builtins.BaseList.Table);
            list.Table.Path = analyzer.builtins.BaseList.Table.Path;
            return list;
        }

        private DictType Register(DictType dictType)
        {
            dictType.Table.addSuper(analyzer.builtins.BaseDict.Table);
            dictType.Table.Path = analyzer.builtins.BaseDict.Table.Path;
            return dictType;
        }
    }
}
