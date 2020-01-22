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

using Pytocs.Core.Syntax;
using Pytocs.Core.Types;
using System.Collections.Generic;
using Name = Pytocs.Core.Syntax.Identifier;

namespace Pytocs.Core.TypeInference
{
    public interface Analyzer
    {
        DataTypeFactory TypeFactory { get; }
        int CalledFunctions { get; set; }
        State GlobalTable { get; }
        HashSet<Name> Resolved { get; }
        HashSet<Name> Unresolved { get; }

        DataType LoadModule(List<Name> name, State state);

        Module GetAstForFile(string file);

        string GetModuleQname(string file);

        Binding CreateBinding(string id, Node node, DataType type, BindingKind kind);

        void addRef(AttributeAccess attr, DataType targetType, ISet<Binding> bs);

        void putRef(Node node, ICollection<Binding> bs);

        void putRef(Node node, Binding bs);

        void AddExpType(Exp node, DataType type);

        void AddUncalled(FunType f);

        void RemoveUncalled(FunType f);

        void pushStack(Exp v);

        void popStack(Exp v);

        bool InStack(Exp v);

        string ModuleName(string path);

        string ExtendPath(string path, string name);

        void AddProblem(Node loc, string msg);

        void AddProblem(string filename, int start, int end, string msg);
    }
}