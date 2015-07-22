using System.Collections.Generic;
using System;
using Pytocs.Types;

namespace Pytocs.TypeInference
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

        public void push(DataType first, DataType second)
        {
            stack.Add(new Pair(first, second));
        }

        public void pop(object first, object second)
        {
            stack.RemoveAt(stack.Count - 1);
        }

        public bool contains(DataType first, DataType second)
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