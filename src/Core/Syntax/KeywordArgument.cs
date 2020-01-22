using System.IO;

namespace Pytocs.Core.Syntax
{
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