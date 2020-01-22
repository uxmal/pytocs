namespace Pytocs.Core.Syntax
{
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