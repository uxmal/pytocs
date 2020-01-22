using System.IO;

namespace Pytocs.Core.Syntax
{
    internal class ListArgument : Argument
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
}