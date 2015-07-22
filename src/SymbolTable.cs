using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs
{
    public class SymbolTable
    {
        private Dictionary<string, Symbol> symbols;

        public SymbolTable()
        {
            symbols = new Dictionary<string, Symbol>();
        }

        public SymbolTable(SymbolTable outer)
            : this()
        { 
        }

        public Symbol GetSymbol(string name)
        {
            Symbol s;
            if (!symbols.TryGetValue(name, out s))
                s = null;
            return s;
        }

        public Symbol Reference(string name)
        {
            Symbol s;
            if (!symbols.TryGetValue(name, out s))
            {
                s = new Symbol { Name = name };
                symbols.Add(s.Name, s);
            }
            return s;
        }
    }
}
