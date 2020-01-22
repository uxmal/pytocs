using System;
using System.IO;

namespace Pytocs.Core.Syntax
{
    ///
    /// virtual-AST node used to represent virtual source locations for builtins
    /// as external urls.
    ///
    public class Url : Exp
    {
        public string url;

        public Url(string url) : base(null, -1, -1)
        {
            this.url = url;
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("<Url:\"{0}\">", url);
        }

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