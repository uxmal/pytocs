using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Pytocs.Syntax
{
    public class Argument : Node
    {
        public readonly Exp name;
        public readonly Exp defval;

        public Argument(Exp name, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.name = name;
            this.defval = null;
        }

        public Argument(Exp name, Exp defval, string filename, int start, int end) : base(filename, start, end)
        {
            this.name = name;
            this.defval = defval;
        }

        public override string ToString()
        {
            var sw = new StringWriter();
            Write(sw);
            return sw.ToString();
        }

        public virtual void Write(TextWriter writer)
        {
            if (name != null)
            {
                name.Write(writer);
                var compFor = defval as CompFor;
                if (compFor != null)
                {
                    writer.Write(" ");
                    compFor.Write(writer);
                    return;
                }
                writer.Write("=");
            }
            if (defval != null)
            {
                defval.Write(writer);
            }
        }
    }

    class ListArgument : Argument
    {
        public ListArgument(Exp t, string filename, int start, int end) : base(t, filename, start, end)
        {
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("*");
            base.Write(writer);
        }
    }

    public class KeywordArgument : Argument
    {
        public KeywordArgument(Exp t, string filename, int start, int end)
            : base(t, filename, start, end)
        {
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("**");
            base.Write(writer);
        }
    }
}
