#region License
//  Copyright 2015-2018 John Källén
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

using Pytocs.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Syntax
{
    /// <summary>
    /// Lexer for Python.
    /// </summary>
    public class Lexer : ILexer
    {
        private string filename;
        private TextReader rdr;
        private Token token;
        private StringBuilder sb;
        private Stack<int> indents;
        private int indent;
        private State st;
        private int nestedParens;
        private int nestedBrackets;
        private int nestedBraces;
        private int posStart;
        private int posEnd;
        private TokenType lastTokenType;
        private bool lastLineEndedInComment;

        public Lexer(string filename, TextReader rdr)
        {
            this.filename = filename;
            this.rdr = rdr;
            this.indents = new Stack<int>();
            this.indents.Push(0);
            this.indent = 0;
            this.LineNumber = 1;
            this.posStart = 0;
            this.posEnd = 0;
            this.st = State.Start;
            this.lastTokenType = TokenType.NONE;
            this.lastLineEndedInComment = false;
        }

        public int LineNumber { get; private set; }

        static Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>()
        {
            { "False", TokenType.False },
            { "class", TokenType.Class },
            { "finally", TokenType.Finally },
            { "is", TokenType.Is },
            { "return", TokenType.Return },
 
            { "None", TokenType.None },
            { "continue", TokenType.Continue },
            { "for", TokenType.For },
            { "lambda", TokenType.Lambda },
            { "try", TokenType.Try },
 
            { "True", TokenType.True },
            { "def", TokenType.Def },
            { "from", TokenType.From },
            { "nonlocal", TokenType.Nonlocal },
            { "while", TokenType.While },
 
            { "and", TokenType.And },
            { "del", TokenType.Del },
            { "global", TokenType.Global },
            { "not", TokenType.Not },
            { "with", TokenType.With },
 
            { "as", TokenType.As },
            { "elif", TokenType.Elif },
            { "if", TokenType.If },
            { "or", TokenType.Or },
            { "yield", TokenType.Yield },
 
            { "assert", TokenType.Assert },
            { "else", TokenType.Else },
            { "import", TokenType.Import },
            { "pass", TokenType.Pass },
 
            { "break", TokenType.Break },
            { "except", TokenType.Except },
            { "in", TokenType.In },
            { "raise", TokenType.Raise },

            { "exec", TokenType.Exec },
            { "async", TokenType.Async },
            { "await", TokenType.Await},
        };
        private bool rawString;
        private bool unicodeString;
        private bool binaryString;
        private bool formatString;
        private int hexDigits;
        private int charConst;

        public Token Get()
        {
            if (token.Type != TokenType.NONE)
            {
                Token t = this.token;
                token = new Token(0, 0, TokenType.NONE, null, 0, 0);
                return t;
            }
            return GetToken();
        }

        public Token Peek()
        {
            if (token.Type != TokenType.NONE)
            {
                return token;
            }
            token = GetToken();
            return token;
        }

        private enum State
        {
            Start,

            Base,
            Id,
            Quote,
            QuoteString,
            Quote2,
            QuoteString3,
            QuoteString3Cr,
            EQuote,
            EQuote2,
            Apos,
            AposString,
            Apos2,
            AposString3,
            AposString3Cr,
            EApos,
            EApos2,

            Zero,
            Decimal,
            Hex,
            Octal,
            Binary,

            Plus,
            Minus,
            Lt,
            Gt,
            Star,
            StarStar,
            Slash,
            SlashSlash,
            Percent,
            Shl,
            Shr,
            Eq,
            Bang,
            Amp,
            Bar,
            Caret,
            Cr,
            BlankLineComment,
            BlankLineCr,
            StringEscape,
            Comment,
            Dot,
            Dot2,
            BackSlash,
            BackSlashCr,
            RawStringPrefix,
            RealFraction,
            RealExponent,
            RealExponentDigits,
            UnicodeStringPrefix,
            StringEscapeHex,
            BinaryStringPrefix,
            FormatStringPrefix,
        }

        private Token GetToken()
        {
            this.sb = new StringBuilder();
            State oldState = (State)(-1);
            for (; ; )
            {
                int c = rdr.Peek();
                char ch = (char)c;
                switch (st)
                {
                case State.Start:
                    switch (ch)
                    {
                    case ' ': Advance(); ++indent; break;
                    case '\t': Advance(); indent = (indent & ~7) + 8; break;
                    case '\r': Transition(State.BlankLineCr); indent = 0; break;
                    case '\n': Advance(); ++LineNumber; indent = 0; break;
                    default:
                        if (ch != '#')
                        {
                            int lastIndent = indents.Peek();
                            if (indent > lastIndent)
                            {
                                st = State.Base;
                                indents.Push(indent);
                                return Token(TokenType.INDENT);
                            }
                            else if (indent < lastIndent)
                            {
                                indents.Pop();
                                lastIndent = indents.Peek();
                                return Token(TokenType.DEDENT, State.Start);
                            }
                        }
                        st = State.Base;
                        break;
                    }
                    break;
                case State.BlankLineCr:
                    ++LineNumber;
                    if (ch == '\n')
                        Advance();
                    st = State.Start;
                    break;
                case State.Base:
                    if (c < 0)
                        return Token(TokenType.EOF);
                    posStart = posEnd;
                    Advance();
                    switch (ch)
                    {
                    case ' ':
                    case '\t':
                        break;
                    case '\r': st = State.Cr; break;
                    case '\n':
                        ++LineNumber;
                        if (IsLogicalNewLine())
                        {
                            return Newline();
                        }
                        ++posStart;
                        break;
                    case '#': st = State.Comment; break;
                    case '+': st = State.Plus; break;
                    case '-': st = State.Minus; break;
                    case '*': st = State.Star; break;
                    case '/': st = State.Slash; break;
                    case '%': st = State.Percent; break;
                    case '<': st = State.Lt; break;
                    case '>': st = State.Gt; break;
                    case '&': st = State.Amp; break;
                    case '|': st = State.Bar; break;
                    case '^': st = State.Caret; break;
                    case '~': return Token(TokenType.OP_TILDE);
                    case '=': st = State.Eq; break;
                    case '!': st = State.Bang; break;
                    case '(': ++nestedParens; return Token(TokenType.LPAREN);
                    case '[': ++nestedBrackets; return Token(TokenType.LBRACKET);
                    case '{': ++nestedBraces; return Token(TokenType.LBRACE);
                    case ')': --nestedParens; return Token(TokenType.RPAREN);
                    case ']': --nestedBrackets; return Token(TokenType.RBRACKET);
                    case '}': --nestedBraces; return Token(TokenType.RBRACE);
                    case ',': return Token(TokenType.COMMA);
                    case ':': return Token(TokenType.COLON);
                    case '.': st = State.Dot; break;
                    case ';': return Token(TokenType.SEMI);
                    case '@': return Token(TokenType.AT);
                    case '0': sb.Append(ch); st = State.Zero; break;
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        sb.Append(ch); st = State.Decimal; break;
                    case 'r':
                    case 'R': sb.Append(ch); st = State.RawStringPrefix; break;
                    case 'u':
                    case 'U': sb.Append(ch); st = State.UnicodeStringPrefix; break;
                    case 'b':
                    case 'B': sb.Append(ch); st = State.BinaryStringPrefix; break;
                    case 'f':
                    case 'F': sb.Append(ch); st = State.FormatStringPrefix; break;
                    case '\"': rawString = false; unicodeString = false; st = State.Quote; break;
                    case '\'': rawString = false; unicodeString = false; st = State.Apos; break;
                    case '\\': st = State.BackSlash; break;
                    default:
                        if (Char.IsLetter(ch) || ch == '_')
                        {
                            sb.Append(ch);
                            st = State.Id;
                            break;
                        }
                        throw Invalid(c, ch);
                    }
                    break;
                case State.BackSlash:
                    switch (ch)
                    {
                    case '\r': ++LineNumber; Transition(State.BackSlashCr); break;
                    case '\n': ++LineNumber; Transition(State.Base); break;
                    default: Invalid(c, ch); break;
                    }
                    break;
                case State.BackSlashCr:
                    switch (ch)
                    {
                    case '\r': ++LineNumber; Transition(st); break;
                    case '\n': Transition(State.Base); break;
                    default: Invalid(c, ch); break;
                    }
                    break;
                case State.Cr:
                    if (ch == '\n')
                        Advance();
                    ++LineNumber;

                    if (IsLogicalNewLine())
                    {
                        return Newline();
                    }
                    else
                    {
                        st = State.Base;
                    }
                    break;
                case State.Comment:
                    switch (ch)
                    {
                    case '\r':
                    case '\n':
                        return Token(TokenType.COMMENT, sb.ToString(), State.Base);
                    default:
                        if (c < 0)
                            st = State.Base;
                        else
                            Accum(ch, st);
                        break;
                    }
                    break;
                case State.Id:
                    if (c >= 0 && (Char.IsLetterOrDigit(ch) || ch == '_'))
                    {
                        Advance();
                        sb.Append(ch);
                        break;
                    }
                    return LookupId();
                case State.Plus:
                    if (c == '=')
                    {
                        return EatChToken(TokenType.ADDEQ);
                    }
                    return Token(TokenType.OP_PLUS);
                case State.Minus:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.SUBEQ);
                    case '>': return EatChToken(TokenType.LARROW);
                    }
                    return Token(TokenType.OP_MINUS);
                case State.Star:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.MULEQ);
                    case '*': Transition(State.StarStar); break;
                    default: return Token(TokenType.OP_STAR);
                    }
                    break;
                case State.StarStar:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.EXPEQ);
                    default: return Token(TokenType.OP_STARSTAR);
                    }
                case State.Slash:
                    switch (ch)
                    {
                    case '/': Transition(State.SlashSlash); break;
                    case '=': return EatChToken(TokenType.DIVEQ);
                    default: return Token(TokenType.OP_SLASH);
                    }
                    break;
                case State.SlashSlash:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.IDIVEQ);
                    default: return Token(TokenType.OP_SLASHSLASH);
                    }
                case State.Percent:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.MODEQ);
                    default: return Token(TokenType.OP_PERCENT);
                    }
                case State.Lt:
                    switch (ch)
                    {
                    case '<': Transition(State.Shl); break;
                    case '=': return EatChToken(TokenType.OP_LE);
                    default: return Token(TokenType.OP_LT);
                    }
                    break;
                case State.Shl:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.SHLEQ);
                    default: return Token(TokenType.OP_SHL);
                    }
                case State.Gt:
                    switch (ch)
                    {
                    case '>': Transition(State.Shr); break;
                    case '=': return EatChToken(TokenType.OP_GE);
                    default: return Token(TokenType.OP_GT);
                    }
                    break;
                case State.Shr:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.SHREQ);
                    default: return Token(TokenType.OP_SHR);
                    }
                case State.Eq:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.OP_EQ);
                    default: return Token(TokenType.EQ);
                    }
                case State.Bang:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.OP_NE);
                    default: throw Invalid(c, ch);
                    }
                case State.Amp:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.ANDEQ);
                    default: return Token(TokenType.OP_AMP);
                    }
                case State.Bar:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.OREQ);
                    default: return Token(TokenType.OP_BAR);
                    }
                case State.Caret:
                    switch (ch)
                    {
                    case '=': return EatChToken(TokenType.XOREQ);
                    default: return Token(TokenType.OP_CARET);
                    }
                case State.Zero:
                    switch (ch)
                    {
                    case 'x':
                    case 'X':
                        Transition(State.Hex); break;
                    case 'o':
                    case 'O':
                        Transition(State.Octal); break;
                    case 'b':
                    case 'B':
                        Transition(State.Binary); break;
                    case 'e':
                    case 'E':
                        Accum(ch, State.RealExponent);
                        break;
                    case 'L':
                    case 'l':
                        return EatChToken(TokenType.LONGINTEGER, Convert.ToInt64(sb.ToString()));
                    case '.':
                        Accum(ch, State.RealFraction);
                        break;
                    case 'j':
                        Advance();
                        return Imaginary();
                    default:
                        if (Char.IsDigit(ch))
                        {
                            Accum(ch, State.Decimal);
                            break;
                        }
                        return Token(TokenType.INTEGER, (object)0);
                    }
                    break;
                case State.Decimal:
                    switch (ch)
                    {
                    case 'L':
                    case 'l':
                        return LongInteger();
                    case 'e':
                    case 'E':
                        Accum(ch, State.RealExponent);
                        break;
                    case '.':
                        Accum(ch, State.RealFraction);
                        break;
                    case 'j':
                        Advance();
                        return Imaginary();
                    default:
                        if (Char.IsDigit(ch))
                        {
                            Accum(ch, State.Decimal);
                            break;
                        }
                        if (int.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                        {
                            return Token(TokenType.INTEGER, num);
                        }
                        else if (long.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var lnum))
                        {
                            return Token(TokenType.LONGINTEGER, lnum);
                        }
                        else if (BigInteger.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var bignum))
                        {
                            return Token(TokenType.LONGINTEGER, bignum);
                        }
                        break;
                    }
                    break;
                case State.RealFraction:
                    if (c < 0)
                    {
                        return Real();
                    }
                    switch (ch)
                    {
                    case 'e':
                    case 'E':
                        Accum(ch, State.RealExponent);
                        break;
                    case 'j':
                    case 'J':
                        Advance();
                        return Imaginary();
                    default:
                        if (Char.IsDigit(ch))
                        {
                            Accum(ch, st);
                        }
                        else
                        {
                            return Real();
                        }
                        break;
                    }
                    break;
                case State.RealExponent:
                    if (c < 0)
                        return Real();
                    if (c == '+' || c == '-')
                    {
                        Accum(ch, State.RealExponentDigits);
                    }
                    else if (char.IsDigit(ch))
                    {
                        Accum(ch, State.RealExponentDigits);
                    }
                    else
                        Invalid(c, ch);
                    break;
                case State.RealExponentDigits:
                    if (c < 0)
                        return Real();
                    if (c == 'j' || c == 'J')
                    {
                        Advance();
                        return Imaginary();
                    }
                    if (char.IsDigit(ch))
                    {
                        Accum(ch, State.RealExponentDigits);
                        break;
                    }
                    return Real();

                case State.Hex:
                    switch (ch)
                    {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                        Accum(ch, State.Hex);
                        break;
                    case 'L':
                    case 'l':
                        Advance();
                        if (long.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var l))
                        {
                            return Token(TokenType.LONGINTEGER, l);
                        }
                        else if (BigInteger.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var big))
                        {
                            return Token(TokenType.LONGINTEGER, big);
                        }
                        else
                        {
                            throw new NotImplementedException($"Unexpected error lexing '{sb}'.");
                        }
                    default:
                        if (int.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
                        {
                            return Token(TokenType.INTEGER, num);
                        }
                        if (long.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var lnum))
                        {
                            return Token(TokenType.LONGINTEGER, lnum);
                        }
                        else if (BigInteger.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bignum))
                        {
                            return Token(TokenType.LONGINTEGER, bignum);
                        }
                        else
                        {
                            throw new FormatException(string.Format(Resources.ErrInvalidHexadecimalString, sb.ToString()));
                        }
                    }
                    break;
                case State.Octal:
                    switch (ch)
                    {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                        Accum(ch, State.Octal);
                        break;
                    case 'L':
                    case 'l':
                        return EatChToken(TokenType.LONGINTEGER, Convert.ToInt64(sb.ToString(), 8));
                    default:
                        return Token(TokenType.INTEGER, Convert.ToInt64(sb.ToString(), 8));
                    }
                    break;
                case State.Binary:
                    switch (ch)
                    {
                    case '0':
                    case '1':
                        Accum(ch, State.Binary);
                        break;
                    case 'L':
                    case 'l':
                        return EatChToken(TokenType.LONGINTEGER, ConvertBinaryToInt(sb.ToString()));
                    default:
                        return Token(TokenType.INTEGER, (long)ConvertBinaryToInt(sb.ToString()));
                    }
                    break;
                case State.BlankLineComment:
                    switch (ch)
                    {
                    case '\r': indent = 0; return Token(TokenType.COMMENT, sb.ToString(), State.Base);
                    case '\n': indent = 0; return Token(TokenType.COMMENT, sb.ToString(), State.Base);
                    default:
                        if (c < 0)
                            return Token(TokenType.EOF);
                        Accum(ch, st);
                        break;
                    }
                    break;

                case State.RawStringPrefix:
                    switch (ch)
                    {
                    case '"': rawString = true; sb.Clear(); Transition(State.Quote); break;
                    case '\'': rawString = true; sb.Clear(); Transition(State.Apos); break;
                    default: st = State.Id; break;
                    }
                    break;
                case State.UnicodeStringPrefix:
                    switch (ch)
                    {
                    case '"': unicodeString = true; sb.Clear(); Transition(State.Quote); break;
                    case '\'': unicodeString = true; sb.Clear(); Transition(State.Apos); break;
                    default: st = State.Id; break;
                    }
                    break;
                case State.BinaryStringPrefix:
                    switch (ch)
                    {
                    case '"': binaryString = true; sb.Clear(); Transition(State.Quote); break;
                    case '\'': binaryString = true; sb.Clear(); Transition(State.Apos); break;
                    default: st = State.Id; break;
                    }
                    break;
                case State.FormatStringPrefix:
                    switch (ch)
                    {
                    case '"': formatString = true; sb.Clear(); Transition(State.Quote); break;
                    case '\'': formatString = true; sb.Clear(); Transition(State.Apos); break;
                    default: st = State.Id; break;
                    }
                    break;

                case State.Quote:
                    switch (ch)
                    {
                    case '"': Transition(State.Quote2); break;
                    case '\\':
                        oldState = State.QuoteString;
                        Accum(ch, State.StringEscape);
                        break;
                    default: Accum(ch, State.QuoteString); break;
                    }
                    break;
                case State.Apos:
                    switch (ch)
                    {
                    case '\'': Transition(State.Apos2); break;
                    case '\\':
                        oldState = State.AposString;
                        Accum(ch, State.StringEscape);
                        break;
                    default: Accum(ch, State.AposString); break;
                    }
                    break;

                case State.Quote2:
                    switch (ch)
                    {
                    case '"': Transition(State.QuoteString3); break;
                    default: return Token(TokenType.STRING, new Str("", filename, posStart, posEnd));
                    }
                    break;
                case State.Apos2:
                    switch (ch)
                    {
                    case '\'': Transition(State.AposString3); break;
                    default: return Token(TokenType.STRING, new Str("", filename, posStart, posEnd));
                    }
                    break;

                case State.QuoteString:
                    switch (ch)
                    {
                    case '"': return EatChToken(TokenType.STRING, CreateStringLiteral(false));
                    case '\\':
                        oldState = st;
                        Accum(ch, (rawString) ? st : State.StringEscape);
                        break;
                    default:
                        if (c < 0)
                            throw Error(Resources.ErrUnexpectedEndOfInput);
                        Accum(ch, State.QuoteString);
                        break;
                    }
                    break;
                case State.AposString:
                    switch (ch)
                    {
                    case '\'': return EatChToken(TokenType.STRING, CreateStringLiteral(false));
                    case '\\':
                        oldState = st;
                        Accum(ch, State.StringEscape);
                        break;
                    default:
                        if (c < 0)
                            throw Error(Resources.ErrUnexpectedEndOfInput);
                        Accum(ch, State.AposString);
                        break;
                    }
                    break;

                case State.QuoteString3:
                    switch (ch)
                    {
                    case '\"': Transition(State.EQuote); break;
                    case '\r': ++LineNumber; AccumString(c, State.QuoteString3Cr); break;
                    case '\n': ++LineNumber; AccumString(c, st); break;
                    default: AccumString(c, st); break;
                    }
                    break;
                case State.AposString3:
                    switch (ch)
                    {
                    case '\'': Transition(State.EApos); break;
                    case '\r': ++LineNumber; AccumString(c, State.AposString3Cr); break;
                    case '\n': ++LineNumber; AccumString(c, st); break;
                    default: AccumString(c, st); break;
                    }
                    break;

                case State.QuoteString3Cr:
                    switch (ch)
                    {
                    case '\'': Transition(State.EQuote); break;
                    case '\r': ++LineNumber; AccumString(c, st); break;
                    default: AccumString(c, State.QuoteString3); break;
                    }
                    break;
                case State.AposString3Cr:
                    switch (ch)
                    {
                    case '\'': Transition(State.EApos); break;
                    case '\r': ++LineNumber; AccumString(c, st); break;
                    default: AccumString(c, State.AposString3); break;
                    }
                    break;

                case State.EQuote:
                    switch (ch)
                    {
                    case '\"': Transition(State.EQuote2); break;
                    default: sb.Append('\"'); AccumString(c, State.QuoteString3); break;
                    }
                    break;
                case State.EApos:
                    switch (ch)
                    {
                    case '\'': Transition(State.EApos2); break;
                    default: sb.Append('\''); AccumString(c, State.AposString3); break;
                    }
                    break;

                case State.EQuote2:
                    switch (ch)
                    {
                    case '\"': return EatChToken(TokenType.STRING, CreateStringLiteral(true));
                    default: sb.Append("\"\""); AccumString(c, State.QuoteString3); break;
                    }
                    break;
                case State.EApos2:
                    switch (ch)
                    {
                    case '\'': return EatChToken(TokenType.STRING, CreateStringLiteral(true));
                    default: sb.Append("\'\'"); AccumString(c, State.AposString3); break;
                    }
                    break;

                case State.StringEscape:
                    if (c < 0)
                    {
                        throw Error(Resources.ErrUnterminatedStringConstant);
                    }
                    Accum(ch, oldState);
                    break;
                //switch (ch)
                //{
                //case '\n': st = oldState; Advance(); break;
                //case '\\': Accum(ch, oldState); break;
                //case '\'': Accum(ch, oldState); break;
                //case '\"': Accum(ch, oldState); break;
                //case 'a': Accum('\a', oldState); break;
                //case 'b': Accum('\b', oldState); break;
                //case 'f': Accum('\f', oldState); break;
                //case 'n': Accum('\n', oldState); break;
                //case 'r': Accum('\r', oldState); break;
                //case 't': Accum('\t', oldState); break;
                //case 'u': sb.Append(ch);  hexDigits = 4; charConst = 0; Transition(State.StringEscapeHex); break;
                //case 'v': Accum('\v', oldState); break;
                //case '0':
                //case '3': 
                //    st = oldState;
                //    sbEscapedCode = new StringBuilder(); 
                //    sbEscapedCode.Append(ch);
                //    Transition(State.StringEscapeDecimal);
                //    break;
                //default: throw new FormatException(string.Format("Unrecognized string escape character {0} (U+{1:X4}).", ch, c));
                //}
                case State.StringEscapeHex:
                    switch (ch)
                    {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        charConst = charConst * 16 + (ch - '0');
                        Advance();
                        if (--hexDigits == 0)
                        {
                            sb.Append((char)charConst);
                            st = oldState;
                        }
                        break;
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                        charConst = charConst * 16 + ((ch - 'A') + 10);
                        Advance();
                        if (--hexDigits == 0)
                        {
                            sb.Append((char)charConst);
                            st = oldState;
                        }
                        break;
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                        charConst = charConst * 16 + ((ch - 'a') + 10);
                        Advance();
                        if (--hexDigits == 0)
                        {
                            sb.Append((char)charConst);
                            st = oldState;
                        }
                        break;
                    }
                    break;
                case State.Dot:
                    switch (ch)
                    {
                    case '.': Transition(State.Dot2); break;
                    default:
                        if (Char.IsDigit(ch))
                        {
                            sb.AppendFormat("0.{0}", ch);
                            Transition(State.RealFraction);
                            break;
                        }
                        return Token(TokenType.DOT);
                    }
                    break;
                case State.Dot2:
                    switch (ch)
                    {
                    case '.': return EatChToken(TokenType.ELLIPSIS);
                    default: this.token = Token(TokenType.DOT); return Token(TokenType.DOT);
                    }
                default:
                    throw Error(string.Format(Resources.ErrUnhandledState, st));
                }
            }
        }

        private Token LongInteger()
        {
            try
            {
                return EatChToken(TokenType.LONGINTEGER,Convert.ToInt64(sb.ToString()));
            }
            catch
            {
                return EatChToken(TokenType.LONGINTEGER, (long)Convert.ToUInt64(sb.ToString()));
            }
        }

        private Exception Error(string errorMsg)
        {
            return new FormatException($"{this.filename}({this.LineNumber}): error: {errorMsg}");
        }

        private Exp CreateStringLiteral(bool longLiteral)
        {
            Exp e;
            if (binaryString)
            {
                e = new Bytes(sb.ToString(), filename, posStart, posEnd);
            }
            else
            {
                e = new Str(sb.ToString(), filename, posStart, posEnd)
                {
                    Raw = rawString,
                    Unicode = unicodeString,
                    Long = longLiteral,
                    Format = formatString,
                };
            }
            binaryString = false;
            rawString = false;
            unicodeString = false;
            longLiteral = false;
            formatString = false;
            return e;
        }

        private long ConvertBinaryToInt(string binaryString)
        {
            long n = 0;
            foreach (var ch in binaryString)
            {
                n = (n << 1) | (ch == '1' ? 1L : 0L);
            }
            return n;
        }

        private bool IsLogicalNewLine()
        {
            return
                nestedParens == 0 &&
                nestedBrackets == 0 &&
                nestedBraces == 0;
        }

        private Token Newline()
        {
            indent = 0;
            return Token(TokenType.NEWLINE, State.Start);
        }

        private void Accum(char ch, State st)
        {
            Advance();
            this.sb.Append(ch);
            this.st = st;
        }

        private void AccumString(int c, State st)
        {
            if (c < 0)
                throw Error(Resources.ErrUnexpectedEndOfStringConstant);
            Advance();
            this.sb.Append((char) c);
            this.st = st;
        }

        private Exception Invalid(int c, char ch)
        {
            throw Error(string.Format(Resources.ErrInvalidCharacter, ch, c));
        }

        private Token EatChToken(TokenType t, object value = null)
        {
            Advance();
            st = State.Base;
            return Token(t, value);
        }

        private Token Token(TokenType t) { return Token(t, null, State.Base); }
        private Token Token(TokenType t, State newState) { return Token(t, null, newState); }
        private Token Token(TokenType t, object value) { return Token(t, value, State.Base); }
        private Token Token(TokenType t, object value, State newState)
        {
            if (t == TokenType.NEWLINE)
            {
                this.lastLineEndedInComment = (this.lastTokenType == TokenType.COMMENT);
            }
            this.st = newState;
            var token = new Token(LineNumber, indent, t, value, posStart, posEnd);
            posStart = posEnd;
            this.lastTokenType = t;
            return token;
        }

        private void Transition(State s)
        {
            this.st = s;
            Advance();
        }

        private void Advance()
        {
            rdr.Read();
            ++posEnd;
        }

        private Token LookupId()
        {
            string value = sb.ToString();
            if (keywords.TryGetValue(value, out var type))
                return Token(type);
            return Token(TokenType.ID, value);
        }

        private Token Real()
        {
            double d;
            try
            {
                d = Convert.ToDouble(sb.ToString(), CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                d = (sb[0] != '-') ? double.PositiveInfinity : double.NegativeInfinity;
            }
            return Token(TokenType.REAL, d, State.Base);
        }

        private Token Imaginary()
        {
            double d;
            try
            {
                d = Convert.ToDouble(sb.ToString(), CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                d = (sb[0] != '-') ? double.PositiveInfinity : double.NegativeInfinity;
            }
            return Token(TokenType.IMAG, d, State.Base);
        }
    }
}
