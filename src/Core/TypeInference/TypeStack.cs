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

using System.Collections.Generic;
using System;
using Pytocs.Core.Types;

namespace Pytocs.Core.TypeInference
{
    public class TypeStack
    {
        class Pair
        {
            public DataType first;
            public DataType second;

            public Pair(DataType first, DataType second)
            {
                this.first = first;
                this.second = second;
            }
        }

        private List<Pair> stack = new List<Pair>();

        public void Push(DataType first, DataType second)
        {
            stack.Add(new Pair(first, second));
        }

        public void Pop(object first, object second)
        {
            stack.RemoveAt(stack.Count - 1);
        }

        public bool Contains(DataType first, DataType second)
        {
            foreach (Pair p in stack)
            {
                if (object.ReferenceEquals(p.first, first) && 
                    object.ReferenceEquals(p.second, second) ||
                    object.ReferenceEquals(p.first, second) &&
                    object.ReferenceEquals(p.second, first))
                {
                    return true;
                }
            }
            return false;
        }
    }
}