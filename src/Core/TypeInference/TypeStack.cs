#region License

//  Copyright 2015-2020 John K�ll�n
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

using Pytocs.Core.Types;
using System.Collections.Generic;

namespace Pytocs.Core.TypeInference
{
    public class TypeStack
    {
        private readonly List<(DataType, DataType)> stack = new List<(DataType, DataType)>();

        public void Push(DataType first, DataType second)
        {
            lock (stack)
            {
                stack.Add((first, second));
            }
        }

        public void Pop(object first, object second)
        {
            lock (stack)
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        public bool Contains(DataType first, DataType second)
        {
            foreach ((DataType first, DataType second) p in stack)
            {
                if (ReferenceEquals(p.first, first) &&
                    ReferenceEquals(p.second, second) ||
                    ReferenceEquals(p.first, second) &&
                    ReferenceEquals(p.second, first))
                {
                    return true;
                }
            }

            return false;
        }
    }
}