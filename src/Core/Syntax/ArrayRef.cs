using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class ArrayRef : Exp
    {
        public readonly Exp array;
        public readonly List<Slice> subs;

        public ArrayRef(Exp array, List<Slice> subs, string filename, int start, int end) : base(filename, start, end)
        {
            this.array = array;
            this.subs = subs;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitArrayRef(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitArrayRef(this);
        }

        public override void Write(TextWriter writer)
        {
            array.Write(writer);
            writer.Write("[");
            foreach (Slice slice in subs)
            {
                slice.Write(writer);
            }

            writer.Write("]");
        }
    }
}