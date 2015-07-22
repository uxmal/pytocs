using System.Collections.Generic;
using System;

namespace Pytocs.TypeInference
{

    public class TypeStack
    {
        class Pair
        {
            public object first;
            public object second;

            public Pair(object first, object second)
            {
                this.first = first;
                this.second = second;
            }
        }

        private List<Pair> stack = new List<Pair>();

        public void push(object first, object second)
        {
            stack.Add(new Pair(first, second));
        }

        public void pop(object first, object second)
        {
            stack.RemoveAt(stack.Count - 1);
        }

        public bool contains(object first, object second)
        {
            foreach (Pair p in stack)
            {
                if (p.first == first && p.second == second ||
                        p.first == second && p.second == first)
                {
                    return true;
                }
            }
            return false;

        }
    }
}