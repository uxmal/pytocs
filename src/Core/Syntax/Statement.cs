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

using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public abstract class Statement : Node
    {
        public string comment;
        public List<Decorator> decorators;

        public Statement(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public abstract T Accept<T>(IStatementVisitor<T> v);

        public abstract void Accept(IStatementVisitor v);

        public sealed override string ToString()
        {
            StringWriter sw = new StringWriter();
            Accept(new PyStatementWriter(sw));
            return sw.ToString();
        }
    }
}