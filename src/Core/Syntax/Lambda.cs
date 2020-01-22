using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class Lambda : Exp
    {
        public List<VarArg> args;
        public Exp body;

        public Lambda(string filename, int start, int end) : base(filename, start, end)
        {
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitLambda(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitLambda(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("lambda");
            writer.Write(" ");
            string sep = "";
            foreach (VarArg v in args)
            {
                writer.Write(sep);
                sep = ",";
                if (v.IsIndexed)
                {
                    writer.Write("*");
                }
                else if (v.IsKeyword)
                {
                    writer.Write("**");
                }

                if (v.name != null)
                {
                    v.name.Write(writer);
                }
            }

            writer.Write(":");
            writer.Write(" ");
            body.Write(writer);
        }
    }
}