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

using System;

namespace Pytocs.Core.Syntax
{
    public abstract class Node
    {
        public /*readonly*/ int End;
        public /*readonly*/ string Filename;
        public string Name;

        public Node Parent;
        public /*readonly*/ int Start;

        public Node(string filename, int start, int end)
        {
            Filename = filename;
            Start = start;
            End = end;
        }

        public virtual Str GetDocString()
        {
            throw new NotImplementedException();
        }
    }
}