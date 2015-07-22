using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public enum Op
    {
        Ge,
        Le,
        Lt,
        Gt,
        Eq,
        Ne,
        In,
        NotIn,
        Is,
        IsNot,
        Xor,
        BitAnd,
        BitOr,
        Shl,
        Shr,
        Add,
        Sub,
        Mul,
        Div,
        IDiv,
        Mod,
        Complement,
        AugAdd,
        AugSub,
        AugMul,
        AugDiv,
        AugMod,
        AugAnd,
        AugOr,
        AugXor,
        AugShl,
        AugShr,
        AugExp,
        AugIDiv,
        Not,
        Exp,
        Assign,
        LogAnd,
        LogOr,
    }

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
