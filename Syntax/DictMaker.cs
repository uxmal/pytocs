using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class DictMaker : Exp
    {
        public DictMaker(string filename, int start, int end) : base(filename, start, end) { }

        public IEnumerable Collection { get; set; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            throw new NotImplementedException();
        }

        public override void Accept(IExpVisitor v)
        {
            throw new NotImplementedException();
        }
    }
}
