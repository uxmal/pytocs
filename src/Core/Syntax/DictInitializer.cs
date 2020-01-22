using System.Collections.Generic;
using System.IO;

namespace Pytocs.Core.Syntax
{
    public class DictInitializer : Exp
    {
        public DictInitializer(List<KeyValuePair<Exp, Exp>> keyValues, string filename, int start, int end)
            : base(filename, start, end)
        {
            KeyValues = keyValues;
        }

        public List<KeyValuePair<Exp, Exp>> KeyValues { get; }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitDictInitializer(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitDictInitializer(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("{");
            writer.Write(" ");
            foreach (KeyValuePair<Exp, Exp> kv in KeyValues)
            {
                if (kv.Key != null)
                {
                    kv.Key.Write(writer);
                    writer.Write(" : ");
                    kv.Value.Write(writer);
                }
                else
                {
                    writer.Write("**");
                    kv.Value.Write(writer);
                }
                writer.Write(", ");
            }
            writer.Write(" ");
            writer.Write("}");
        }
    }
}