namespace Pytocs.Core.Syntax
{
    public static class OpTools
    {
        public static bool IsBoolean(this Op op)
        {
            return op == Op.Eq ||
                   //op == Op.Eqv ||
                   //op == Op.Equal ||
                   op == Op.Lt ||
                   op == Op.Gt ||
                   op == Op.Ne ||
                   //op == Op.NotEqual ||
                   //op == Op.NotEq ||
                   op == Op.Le ||
                   op == Op.Ge ||
                   op == Op.In ||
                   op == Op.NotIn;
        }
    }
}