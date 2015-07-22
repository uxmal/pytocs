using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class DottedName : Exp
    {
        public List<Identifier> segs;

        public DottedName(List<Identifier> segs, string filename, int start, int end) : base(filename, start, end)
        {
            this.segs = segs;
        }

        public override void Write(TextWriter writer)
        {
            writer.Write(string.Join(".", segs.Select(s => s.Name)));
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitDottedName(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitDottedName(this);
        }
    }
}
