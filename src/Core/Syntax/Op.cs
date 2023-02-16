#region License
//  Copyright 2015-2021 John Källén
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Core.Syntax
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
        MatMul,
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
        AugMatMul,
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
