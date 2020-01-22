namespace Pytocs.Core.Syntax
{
    public abstract class CompIter : Exp
    {
        public CompIter next;

        public CompIter(string filename, int start, int end) : base(filename, start, end)
        {
        }
    }
}