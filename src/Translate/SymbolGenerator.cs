#region License
//  Copyright 2015 John Källén
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    /// <summary>
    /// Generates symbols unique for a given scope to avoid colissions.
    /// </summary>
    public class SymbolGenerator
    {
        private Dictionary<string, LocalSymbol> autos;
        private List<Dictionary<string, CodeExpression>> stack;

        public SymbolGenerator()
        {
            this.autos = new Dictionary<string, LocalSymbol>();
            this.stack = new List<Dictionary<string, CodeExpression>>();
        }

        public CodeVariableReferenceExpression GenSymParameter(string prefix, CodeTypeReference type)
        {
            return GenSymAutomatic(prefix, type, true);
        }

        public CodeVariableReferenceExpression GenSymLocal(string prefix, CodeTypeReference type)
        {
            return GenSymAutomatic(prefix, type, false);
        }

        public CodeVariableReferenceExpression GenSymAutomatic(string prefix, CodeTypeReference type, bool parameter)
        {
            int i = 1;
            while (autos.Select(l => l.Key).Contains(prefix + i))
                ++i;
            var name = prefix + i;
            EnsureLocalVariable(name, type, parameter);
            return new CodeVariableReferenceExpression(name);
        }

        public LocalSymbol EnsureLocalVariable(string name, CodeTypeReference type, bool parameter)
        {
            LocalSymbol local;
            if (!autos.TryGetValue(name, out local))
            {
                local = new LocalSymbol(name, type, parameter);
                autos.Add(name, local);
            }
            return local;
        }

        public CodeExpression MapLocalReference(string id)
        {
            for (int i = stack.Count - 1; i >= 0; --i)
            {
                CodeExpression exp;
                if (stack[i].TryGetValue(id, out exp))
                    return exp;
            }
            return new CodeVariableReferenceExpression(id);
        }

        public void PushIdMappings(Dictionary<string, CodeExpression> mappings)
        {
            stack.Add(mappings);
        }

        public void PopIdMappings()
        {
            stack.RemoveAt(stack.Count - 1);
        }
    }
}
