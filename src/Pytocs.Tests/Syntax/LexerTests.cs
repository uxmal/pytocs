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

#if DEBUG
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.Syntax;
using System.ComponentModel.DataAnnotations;

namespace Pytocs.UnitTests.Syntax
{
    public class LexerTests
    {
        private Lexer lexer;

        public LexerTests()
        {
            lexer = default!;
        }

        private Token Lex(string str)
        {
            StringReader rdr = new StringReader(str);
            lexer = new Lexer("foo.py", rdr);
            return lexer.Get();
        }

        private Token LexMore()
        {
            if (lexer is null)
                throw new InvalidOperationException("You must call Lex() first.");
            return lexer.Get();
        }

        private void Lex(string str, params TokenType[] tokens)
        {
            StringReader rdr = new StringReader(str);
            lexer = new Lexer("foo.py", rdr);
            Token tok = new Token();
            foreach (var exp in tokens)
            {
                tok = lexer.Get();
                Assert.Equal(exp, tok.Type);
            }
            Assert.Equal(TokenType.EOF, tok.Type);
        }

        [Fact]
        public void LexId()
        {
            Assert.Equal(new Token(0, 0, TokenType.ID, "hoo", null, 0, 3), Lex("hoo"));
        }

        [Fact]
        public void LexOps()
        {
            Assert.Equal(TokenType.OP_PLUS, Lex("+").Type);
            Assert.Equal(TokenType.OP_MINUS, Lex("-").Type);
            Assert.Equal(TokenType.OP_STAR, Lex("*").Type);
            Assert.Equal(TokenType.OP_STARSTAR, Lex("**").Type);
            Assert.Equal(TokenType.OP_SLASH, Lex("/").Type);
            Assert.Equal(TokenType.OP_SLASHSLASH, Lex("//").Type);
            Assert.Equal(TokenType.OP_PERCENT, Lex("%").Type);

            Assert.Equal(TokenType.OP_SHL, Lex("<<").Type);
            Assert.Equal(TokenType.OP_SHR, Lex(">>").Type);
            Assert.Equal(TokenType.OP_AMP, Lex("&").Type);
            Assert.Equal(TokenType.OP_BAR, Lex("|").Type);
            Assert.Equal(TokenType.OP_CARET, Lex("^").Type);
            Assert.Equal(TokenType.OP_TILDE, Lex("~").Type);

            Assert.Equal(TokenType.OP_LT, Lex("<").Type);
            Assert.Equal(TokenType.OP_GT, Lex(">").Type);
            Assert.Equal(TokenType.OP_LE, Lex("<=").Type);
            Assert.Equal(TokenType.OP_GE, Lex(">=").Type);
            Assert.Equal(TokenType.OP_EQ, Lex("==").Type);
            Assert.Equal(TokenType.OP_NE, Lex("!=").Type);

            Assert.Equal(TokenType.LPAREN, Lex("(").Type);
            Assert.Equal(TokenType.RPAREN, Lex(")").Type);
            Assert.Equal(TokenType.LBRACKET, Lex("[").Type);
            Assert.Equal(TokenType.RBRACKET, Lex("]").Type);
            Assert.Equal(TokenType.LBRACE, Lex("{").Type);
            Assert.Equal(TokenType.RBRACE, Lex("}").Type);

            Assert.Equal(TokenType.COMMA, Lex(",").Type);
            Assert.Equal(TokenType.COLON, Lex(":").Type);
            Assert.Equal(TokenType.DOT, Lex(".").Type);
            Assert.Equal(TokenType.SEMI, Lex(";").Type);
            Assert.Equal(TokenType.AT, Lex("@").Type);
            Assert.Equal(TokenType.EQ, Lex("=").Type);

            Assert.Equal(TokenType.ADDEQ, Lex("+=").Type);
            Assert.Equal(TokenType.SUBEQ, Lex("-=").Type);
            Assert.Equal(TokenType.MULEQ, Lex("*=").Type);
            Assert.Equal(TokenType.DIVEQ, Lex("/=").Type);
            Assert.Equal(TokenType.IDIVEQ, Lex("//=").Type);
            Assert.Equal(TokenType.MODEQ, Lex("%=").Type);

            Assert.Equal(TokenType.ANDEQ, Lex("&=").Type);
            Assert.Equal(TokenType.OREQ, Lex("|=").Type);
            Assert.Equal(TokenType.XOREQ, Lex("^=").Type);
            Assert.Equal(TokenType.SHREQ, Lex(">>=").Type);
            Assert.Equal(TokenType.SHLEQ, Lex("<<=").Type);
            Assert.Equal(TokenType.EXPEQ, Lex("**=").Type);
        }

        [Fact]
        public void LexDef()
        {
            Lex("def foo(bar):\n return 1",
                TokenType.Def, TokenType.ID, TokenType.LPAREN, TokenType.ID, TokenType.RPAREN, TokenType.COLON, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.Return, TokenType.INTEGER, TokenType.EOF);
        }

        [Fact]
        public void LexNestedIndents()
        {
            Lex(
               "if hello:\n" +
               "  if goodbye:\n" +
               "     return 3\n" +
               "print 4\n" +
               "return 4\n",
               TokenType.If, TokenType.ID, TokenType.COLON, TokenType.NEWLINE,
               TokenType.INDENT, TokenType.If, TokenType.ID, TokenType.COLON, TokenType.NEWLINE,
               TokenType.INDENT, TokenType.Return, TokenType.INTEGER, TokenType.NEWLINE,
               TokenType.DEDENT, TokenType.DEDENT, TokenType.ID, TokenType.INTEGER, TokenType.NEWLINE,
               TokenType.Return, TokenType.INTEGER, TokenType.NEWLINE, TokenType.EOF);
        }

        [Fact]
        public void LexComment()
        {
            Lex("  foo # hello\nfoo\n",
                TokenType.INDENT, TokenType.ID, TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.DEDENT, TokenType.ID, TokenType.NEWLINE, TokenType.EOF);
        }
        [Fact]
        public void LexBlankLineComment()
        {
            Lex("  # hello\nfoo\n",
                TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.ID, TokenType.NEWLINE, TokenType.EOF);
        }

        [Fact]
        public void LexInt()
        {
            Assert.Equal(0, (int)Lex("0").NumericValue!);
            Assert.Equal(1, (int)Lex("1").NumericValue!);
            Assert.Equal(30, (int)Lex("30").NumericValue!);
            Assert.Equal(0xF, (int)Lex("0xF").NumericValue!);
            Assert.Equal(0xed, (int)Lex("0xed").NumericValue!);
            Assert.Equal(10, (long)Lex("0o12").NumericValue!);
            Assert.Equal(13, (long)Lex("0O15").NumericValue!);
            Assert.Equal(0xA, (int)Lex("0b1_010").NumericValue!);
            Assert.Equal(0B1010, (int)Lex("0B1010").NumericValue!);
        }

        private string LexString(string pyStr)
        {
            return ((Str)Lex(pyStr).Value!).Value;
        }

        [Fact]
        public void LexStrings()
        {
            //Assert.Equal("", Lex("\"\"").Value);
            //Assert.Equal("a", Lex("\"a\"").Value);
            //Assert.Equal("a", Lex("'a'").Value);
            //Assert.Equal("\"a\"", Lex("'\"a\"'").Value);
            //Assert.Equal("ab", Lex("'a\\\nb'").Value);
            //Assert.Equal("\\", Lex(@"'\\'").Value);
            Assert.Equal("\\'", LexString(@"'\''"));
            Assert.Equal("\\\"", LexString(@"'\""'"));
            Assert.Equal("\\a", LexString(@"'\a'"));
            Assert.Equal("\\b", LexString(@"'\b'"));
            Assert.Equal("\\f", LexString(@"'\f'"));
            Assert.Equal("\\n", LexString(@"'\n'"));
            Assert.Equal("\\r", LexString(@"'\r'"));
            Assert.Equal("\\v", LexString(@"'\v'"));
            Assert.Equal("a", LexString("\"\"\"a\"\"\""));
            Assert.Equal("a", LexString("'''a'''"));
            //\ooo 	Character with octal value ooo 	(1,3)
            //\xhh 	Character with hex value hh 	(2,3)

            //Escape sequences only recognized in string literals are:
            //Escape Sequence 	Meaning 	Notes
            //\N{name} 	Character named name in the Unicode database 	(4)
            //\uxxxx 	Character with 16-bit hex value xxxx 	(5)
            //\Uxxxxxxxx 	Character with 32-bit hex value xxxxxxxx 	(6)
        }

        [Fact]
        public void LexArrow()
        {
            Assert.Equal(TokenType.LARROW, Lex("->").Type);
        }

        [Fact]
        public void LexEllipsis()
        {
            Lex("...", TokenType.ELLIPSIS, TokenType.EOF);
            Lex("..", TokenType.DOT, TokenType.DOT, TokenType.EOF);
        }

        [Fact]
        public void Lex_Regression1()
        {
            Lex("'']", TokenType.STRING, TokenType.RBRACKET, TokenType.EOF);
        }

        [Fact]
        public void Lex_LineExtension()
        {
            Lex("foo \\\r\n  bar", TokenType.ID, TokenType.ID, TokenType.EOF);
            Assert.Equal(2, lexer.LineNumber);
        }

        [Fact]
        public void Lex_StrConstant()
        {
            Lex("\"\"\n", TokenType.STRING, TokenType.NEWLINE, TokenType.EOF);
        }

        [Fact]
        public void Lex_LongInteger()
        {
            Lex("1L", TokenType.LONGINTEGER, TokenType.EOF);
        }

        [Fact]
        public void Lex_LongZero()
        {
            Lex("0L", TokenType.LONGINTEGER, TokenType.EOF);
        }

        [Fact]
        public void Lex_RawString()
        {
            var token = LexString(@"r'\''");
            Assert.Equal("\\'", token);
        }

        [Fact]
        public void Lex_DecimalEscape()
        {
            var token = LexString(@"'\33'");
            Assert.Equal("\\33", token);
        }

        [Fact(DisplayName = nameof(Lex_RealLiteral))]
        public void Lex_RealLiteral()
        {
            var token = Lex("0.1");
            Assert.Equal(TokenType.REAL, token.Type);
            Assert.Equal(0.1, token.NumericValue!);
        }

        [Fact]
        public void Lex_UnicodeString()
        {
            var token = Lex("u'foo'");
            Assert.Equal(TokenType.STRING, token.Type);
            Assert.Equal("foo", ((Str)token.Value!).Value);
        }

        [Fact]
        public void Lex_UnicodeStringConstant()
        {
            var token = Lex("u'\u00e9'");
            Assert.Equal(TokenType.STRING, token.Type);
            Assert.Equal("é", ((Str)token.Value!).Value);
        }

        [Fact]
        public void Lex_Comment()
        {
            var token = Lex("# Hello\n");
            Assert.Equal(TokenType.COMMENT, token.Type);
            Assert.Equal(" Hello", token.Value);
        }

        [Fact]
        public void Lex_Indented()
        {
            Lex("if x :\n    hi\n",
                TokenType.If, TokenType.ID, TokenType.COLON, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.ID, TokenType.NEWLINE, TokenType.DEDENT,
                TokenType.EOF);
        }

        [Fact]
        public void Lex_IndentedComment()
        {
            Lex("if x :\n    #foo\n    hi\n",
                TokenType.If, TokenType.ID, TokenType.COLON, TokenType.NEWLINE,
                TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.ID, TokenType.NEWLINE,
                TokenType.DEDENT,
                TokenType.EOF);

        }

        [Fact]
        public void Lex_UnevenComments()
        {
            Lex("#foo\n  #bar\n#baz\n",
                TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.COMMENT, TokenType.NEWLINE, TokenType.EOF);
        }

        [Fact]
        public void Lex_AddEq()
        {
            Lex("+=", TokenType.ADDEQ, TokenType.EOF);
        }

        [Fact]
        public void Lex_StringWithCrLf()
        {
            var t = LexString("'\\r\\n'");
            Assert.Equal("\\r\\n", t);
        }

        [Fact]
        public void Lex_BinaryString()
        {
            var t = Lex("b'\x00'");
            Assert.Equal(TokenType.STRING, t.Type);
            Assert.Equal("\x00", ((Bytes)t.Value!).s);
        }

        [Fact]
        public void Lex_Position()
        {
            var t = Lex("    x");
            Assert.Equal(0, t.Start);
            Assert.Equal(4, t.End);
            t = LexMore();
            Assert.Equal(4, t.Start);
            Assert.Equal(5, t.End);
        }

        [Fact]
        public void Lex_PositionCrLf()
        {
            var t = Lex("\r\nx");
            Assert.Equal(2, t.Start);   // Expected start at 2
            Assert.Equal(3, t.End);     // Expected end at 3
        }

        [Fact]
        public void Lex_Indent()
        {
            Lex("def foo():\n    return\n",
                TokenType.Def, TokenType.ID, TokenType.LPAREN, TokenType.RPAREN, TokenType.COLON, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.Return, TokenType.NEWLINE, TokenType.DEDENT, TokenType.EOF);
        }

        [Fact]
        public void Lex_Regression2()
        {
            Lex(@"r'\'', 'I'",
                TokenType.STRING, TokenType.COMMA, TokenType.STRING, TokenType.EOF);
        }
        
        [Fact]
        public void Lex_FloatConstant()
        {
            Lex(".68",
                TokenType.REAL, TokenType.EOF);
        }

        [Fact]
        public void Lex_Mixed_ByteStrings_Strings()
        {
            var tok = Lex("b\"bytes\" \"chars\"");
            Assert.IsAssignableFrom<Bytes>(tok.Value);
            tok = LexMore();
            Assert.IsAssignableFrom<Str>(tok.Value);
        }

        [Fact]
        public void Lex_FString()
        {
            var tok = Lex("f'Message: {msg}'");
            var str = (Str)tok.Value!;
            Assert.True(str.Format);
        }

        [Fact]
        public void Lex_Float_ScientificNotation()
        {
            var tok = Lex("1E-5");
            var str = (double)tok.NumericValue!;
            Assert.Equal("1E-05", str.ToString());
        }

        [Fact]
        public void Lex_Float_ScientificNotation_Zero()
        {
            var tok = Lex("0E0");
            var str = (double)tok.NumericValue!;
            Assert.Equal("0", str.ToString());
        }

        [Fact]
        public void Lex_Github_26()
        {
            var tok = Lex("1e300000");
            Assert.Equal(TokenType.REAL, tok.Type);
            Assert.Equal(double.PositiveInfinity, (double)tok.NumericValue!);
        }

        [Fact]
        public void Lex_ImaginaryConstant()
        {
            var tok = Lex("3j");
            Assert.Equal(TokenType.IMAG, tok.Type);
            Assert.Equal("3", (string)tok.Value!);
            Assert.Equal(3.0, (double)tok.NumericValue!);
        }

        [Fact]
        public void Lex_Numeric_With_Visual_Separators()
        {
            var tok = Lex("3_000");
            Assert.Equal(TokenType.INTEGER, tok.Type);
            Assert.Equal("3_000", tok.Value);
            Assert.Equal(3000, tok.NumericValue!);
        }

        [Fact]
        public void Lex_RawString_QuotedString()
        {
            var tok = Lex(@"r'\''");
            Assert.Equal(@"r""\'""", tok.Value!.ToString());
        }

        [Fact]
        public void Lex_BinaryRawString()
        {
            var tok = Lex("br'foo'");
            Assert.Equal("br\"foo\"", tok.Value!.ToString());
        }

        [Fact]
        public void Lex_BinaryRawString_rb()
        {
            var tok = Lex("rb'foo'");
            Assert.Equal("br\"foo\"", tok.Value!.ToString());
        }

        [Fact]
        public void Lex_integer_literal()
        {
            var tok = Lex("2_147_483_648");
            Assert.Equal("2_147_483_648", tok.Value!.ToString());
            Assert.Equal("2147483648", tok.NumericValue!.ToString());
        }

        [Fact]
        public void Lex_regression1()
        {
            var tok = Lex("bridge");
            Assert.Equal("bridge", tok.Value!.ToString());
        }

        [Fact]
        public void Lex_binstring_string()
        {
            var tok = Lex("b''('bar'");
            Assert.True(tok.Value is Bytes);
            tok = LexMore();
            Assert.Equal(TokenType.LPAREN, tok.Type);
            tok = LexMore();
            Assert.Equal(TokenType.STRING, tok.Type);
            Assert.True(tok.Value is Str);
        }

        [Fact]
        public void Lex_assignment_expression()
        {
            var tok = Lex(":=");
            Assert.Equal(TokenType.COLONEQ, tok.Type);
        }

        [Fact(DisplayName = nameof(Lex_Github_89))]
        public void Lex_Github_89()
        {
            var tok = Lex("0b_0100_0000");
            Assert.Equal(TokenType.INTEGER, tok.Type);
            Assert.Equal("0b_0100_0000", tok.Value);
            tok = LexMore();
            Assert.Equal(TokenType.EOF, tok.Type);
        }
    }
}
#endif
