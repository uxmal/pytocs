using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class Application : Exp
    {
        public readonly List<Argument> args;
        public readonly Exp fn;
        public readonly List<Argument> keywords;
        public readonly Exp kwargs;
        public readonly Exp stargs;

        public Application(Exp fn, List<Argument> args, List<Argument> keywords, Exp stargs, Exp kwargs,
            string filename, int start, int end) : base(filename, start, end)
        {
            this.fn = fn;
            this.args = args;
            this.keywords = keywords;
            this.stargs = stargs;
            this.kwargs = kwargs;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitApplication(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitApplication(this);
        }

        public override void Write(TextWriter writer)
        {
            fn.Write(writer);
            writer.Write("(");
            string sep = "";
            foreach (Argument arg in args)
            {
                writer.Write(sep);
                arg.Write(writer);
                sep = ",";
            }
            foreach (Argument arg in keywords)
            {
                writer.Write(sep);
                arg.Write(writer);
                sep = ",";
            }
            if (stargs != null)
            {
                writer.Write(sep);
                writer.Write("*");
                stargs.Write(writer);
                sep = ",";
            }
            if (kwargs != null)
            {
                writer.Write(sep);
                writer.Write("**");
                kwargs.Write(writer);
            }
            writer.Write(")");
        }
    }
}