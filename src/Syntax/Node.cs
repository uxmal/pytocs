using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Syntax
{
    public abstract class Node
    {
        public /*readonly*/ string Filename;
        public /*readonly*/ int Start;
        public /*readonly*/ int End;

        public Node Parent;
        public string Name;

        public Node(string filename, int start, int end)
        {
            this.Filename = filename;
            this.Start = start;
            this.End = end;
        }

        public virtual Str GetDocString() { throw new NotImplementedException();  }
    }
}
