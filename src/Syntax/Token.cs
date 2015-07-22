using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Syntax
{
    public struct Token
    {
        public readonly int LineNumber;
        public readonly TokenType Type;
        public readonly object Value;
        public readonly int Start;
        public readonly int End;

        public Token(int lineNumber, TokenType type, object value, int start, int end)
        {
            this.LineNumber = lineNumber;
            this.Type = type;
            this.Value = value;
            this.Start = start;
            this.End = end;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is Token)
            {
                var oToken = (Token) obj;
                return this == oToken;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = Type.GetHashCode();
            if (Value == null)
                return h;
            return h * 17 | Value.GetHashCode();
        }

        public static bool operator == (Token a, Token b)
        {
            return a.Type == b.Type && object.Equals(a.Value, b.Value);
        }

        public static bool operator != (Token a, Token b)
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

    }
}
