﻿#region License

//  Copyright 2015-2020 John Källén
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

#endregion License

namespace Pytocs.Core.Syntax
{
    public struct Token
    {
        public readonly int LineNumber;
        public readonly int Indent;
        public readonly TokenType Type;
        public readonly object Value;
        public readonly object NumericValue;
        public readonly int Start;
        public readonly int End;

        public Token(int lineNumber, int indent, TokenType type, object value, object numericValue, int start, int end)
        {
            LineNumber = lineNumber;
            Indent = indent;
            Type = type;
            Value = value;
            NumericValue = numericValue;
            Start = start;
            End = end;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is Token)
            {
                Token oToken = (Token)obj;
                return this == oToken;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = Type.GetHashCode();
            if (Value == null)
            {
                return h;
            }

            return (h * 17) | Value.GetHashCode();
        }

        public static bool operator ==(Token a, Token b)
        {
            return a.Type == b.Type && Equals(a.Value, b.Value);
        }

        public static bool operator !=(Token a, Token b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format(
                Value != null ? "{{{0} ({1})}}" : "{{{0}}}",
                Type,
                Value);
        }
    }

    public enum TokenType
    {
        EOF = -1,
        NONE = 0,

        ID = 1,
        INTEGER = 2,
        REAL = 3,
        IMAG = 4,

        INDENT,
        DEDENT,
        NEWLINE,

        STRING,

        OP_PLUS,
        OP_MINUS,
        OP_STAR,
        OP_STARSTAR,
        OP_SLASH,
        OP_SLASHSLASH,
        OP_PERCENT,

        OP_SHL,
        OP_SHR,
        OP_AMP,
        OP_BAR,
        OP_CARET,
        OP_TILDE,

        OP_LT,
        OP_GT,
        OP_LE,
        OP_GE,
        OP_EQ,
        OP_NE,

        LPAREN,
        RPAREN,
        LBRACKET,
        RBRACKET,
        LBRACE,
        RBRACE,

        COMMA,
        COLON,
        DOT,
        SEMI,
        AT,
        EQ,

        ADDEQ,
        SUBEQ,
        MULEQ,
        DIVEQ,
        IDIVEQ,
        MODEQ,

        ANDEQ,
        OREQ,
        XOREQ,
        SHREQ,
        SHLEQ,
        EXPEQ,

        False,
        Class,
        Finally,
        Is,
        Return,

        None,
        Continue,
        For,
        Lambda,
        Try,

        True,
        Def,
        From,
        Nonlocal,
        While,

        And,
        Del,
        Global,
        Not,
        With,

        As,
        Elif,
        If,
        Or,
        Yield,

        Assert,
        Else,
        Import,
        Pass,

        Break,
        Except,
        In,
        Raise,
        LARROW,

        ELLIPSIS,
        LONGINTEGER,
        Exec,
        COMMENT,
        Async,

        Await
    }
}