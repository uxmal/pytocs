#region License
//  Copyright 2015 John Källén
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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Syntax
{
    [TestFixture]
    public class LexerTests
    {
        private Lexer lexer;

        [SetUp]
        public void Setup()
        {
            lexer = null;
        }

        private Token Lex(string str)
        {
            StringReader rdr = new StringReader(str);
            lexer = new Lexer("foo.py", rdr);
            return lexer.Get();
        }

        private Token LexMore()
        {
            if (lexer == null)
                throw new InvalidOperationException("Must call Lex() first.");
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
                Assert.AreEqual(exp, tok.Type);
            }
            Assert.AreEqual(TokenType.EOF, tok.Type);
        }

        [Test]
        public void LexId()
        {
            Assert.AreEqual(new Token(0, TokenType.ID, "hoo", 0, 3), Lex("hoo"));
        }

        [Test]
        public void LexOps()
        {
            Assert.AreEqual(TokenType.OP_PLUS, Lex("+").Type);
            Assert.AreEqual(TokenType.OP_MINUS, Lex("-").Type);
            Assert.AreEqual(TokenType.OP_STAR, Lex("*").Type);
            Assert.AreEqual(TokenType.OP_STARSTAR, Lex("**").Type);
            Assert.AreEqual(TokenType.OP_SLASH, Lex("/").Type);
            Assert.AreEqual(TokenType.OP_SLASHSLASH, Lex("//").Type);
            Assert.AreEqual(TokenType.OP_PERCENT, Lex("%").Type);

            Assert.AreEqual(TokenType.OP_SHL, Lex("<<").Type);
            Assert.AreEqual(TokenType.OP_SHR, Lex(">>").Type);
            Assert.AreEqual(TokenType.OP_AMP, Lex("&").Type);
            Assert.AreEqual(TokenType.OP_BAR, Lex("|").Type);
            Assert.AreEqual(TokenType.OP_CARET, Lex("^").Type);
            Assert.AreEqual(TokenType.OP_TILDE, Lex("~").Type);

            Assert.AreEqual(TokenType.OP_LT, Lex("<").Type);
            Assert.AreEqual(TokenType.OP_GT, Lex(">").Type);
            Assert.AreEqual(TokenType.OP_LE, Lex("<=").Type);
            Assert.AreEqual(TokenType.OP_GE, Lex(">=").Type);
            Assert.AreEqual(TokenType.OP_EQ, Lex("==").Type);
            Assert.AreEqual(TokenType.OP_NE, Lex("!=").Type);

            Assert.AreEqual(TokenType.LPAREN, Lex("(").Type);
            Assert.AreEqual(TokenType.RPAREN, Lex(")").Type);
            Assert.AreEqual(TokenType.LBRACKET, Lex("[").Type);
            Assert.AreEqual(TokenType.RBRACKET, Lex("]").Type);
            Assert.AreEqual(TokenType.LBRACE, Lex("{").Type);
            Assert.AreEqual(TokenType.RBRACE, Lex("}").Type);

            Assert.AreEqual(TokenType.COMMA, Lex(",").Type);
            Assert.AreEqual(TokenType.COLON, Lex(":").Type);
            Assert.AreEqual(TokenType.DOT, Lex(".").Type);
            Assert.AreEqual(TokenType.SEMI, Lex(";").Type);
            Assert.AreEqual(TokenType.AT, Lex("@").Type);
            Assert.AreEqual(TokenType.EQ, Lex("=").Type);

            Assert.AreEqual(TokenType.ADDEQ, Lex("+=").Type);
            Assert.AreEqual(TokenType.SUBEQ, Lex("-=").Type);
            Assert.AreEqual(TokenType.MULEQ, Lex("*=").Type);
            Assert.AreEqual(TokenType.DIVEQ, Lex("/=").Type);
            Assert.AreEqual(TokenType.IDIVEQ, Lex("//=").Type);
            Assert.AreEqual(TokenType.MODEQ, Lex("%=").Type);

            Assert.AreEqual(TokenType.ANDEQ, Lex("&=").Type);
            Assert.AreEqual(TokenType.OREQ, Lex("|=").Type);
            Assert.AreEqual(TokenType.XOREQ, Lex("^=").Type);
            Assert.AreEqual(TokenType.SHREQ, Lex(">>=").Type);
            Assert.AreEqual(TokenType.SHLEQ, Lex("<<=").Type);
            Assert.AreEqual(TokenType.EXPEQ, Lex("**=").Type);
        }

        [Test]
        public void LexDef()
        {
            Lex("def foo(bar):\n return 1",
                TokenType.Def, TokenType.ID, TokenType.LPAREN, TokenType.ID, TokenType.RPAREN, TokenType.COLON, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.Return, TokenType.INTEGER, TokenType.EOF);
        }

        [Test]
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

        [Test]
        public void LexComment()
        {
            Lex("  foo # hello\nfoo\n",
                TokenType.INDENT, TokenType.ID, TokenType.COMMENT, TokenType.NEWLINE, TokenType.DEDENT, TokenType.ID, TokenType.NEWLINE, TokenType.EOF);
        }
        [Test]
        public void LexBlankLineComment()
        {
            Lex("  # hello\nfoo\n",
                TokenType.COMMENT, TokenType.NEWLINE, TokenType.ID, TokenType.NEWLINE, TokenType.EOF);
        }

        [Test]
        public void LexInt()
        {
            Assert.AreEqual(0, (int)Lex("0").Value);
            Assert.AreEqual(1, (int)Lex("1").Value);
            Assert.AreEqual(30, (int)Lex("30").Value);
            Assert.AreEqual(0xF, (int)Lex("0xF").Value);
            Assert.AreEqual(0xed, (int)Lex("0xed").Value);
            Assert.AreEqual(10, (int)Lex("0o12").Value);
            Assert.AreEqual(13, (int)Lex("0O15").Value);
            Assert.AreEqual(0xA, (int)Lex("0b1010").Value);
            Assert.AreEqual(0xA, (int)Lex("0B1010").Value);
        }

        private string LexString(string pyStr)
        {
            return ((Str)Lex(pyStr).Value).s;
        }

        [Test]
        public void LexStrings()
        {
            //Assert.AreEqual("", Lex("\"\"").Value);
            //Assert.AreEqual("a", Lex("\"a\"").Value);
            //Assert.AreEqual("a", Lex("'a'").Value);
            //Assert.AreEqual("\"a\"", Lex("'\"a\"'").Value);
            //Assert.AreEqual("ab", Lex("'a\\\nb'").Value);
            //Assert.AreEqual("\\", Lex(@"'\\'").Value);
            Assert.AreEqual("\\'", LexString(@"'\''"));
            Assert.AreEqual("\\\"", LexString(@"'\""'"));
            Assert.AreEqual("\\a", LexString(@"'\a'"));
            Assert.AreEqual("\\b", LexString(@"'\b'"));
            Assert.AreEqual("\\f", LexString(@"'\f'"));
            Assert.AreEqual("\\n", LexString(@"'\n'"));
            Assert.AreEqual("\\r", LexString(@"'\r'"));
            Assert.AreEqual("\\v", LexString(@"'\v'"));
            Assert.AreEqual("a", LexString("\"\"\"a\"\"\""));
            Assert.AreEqual("a", LexString("'''a'''"));
            //\ooo 	Character with octal value ooo 	(1,3)
            //\xhh 	Character with hex value hh 	(2,3)

            //Escape sequences only recognized in string literals are:
            //Escape Sequence 	Meaning 	Notes
            //\N{name} 	Character named name in the Unicode database 	(4)
            //\uxxxx 	Character with 16-bit hex value xxxx 	(5)
            //\Uxxxxxxxx 	Character with 32-bit hex value xxxxxxxx 	(6)
        }

        [Test]
        public void LexArrow()
        {
            Assert.AreEqual(TokenType.LARROW, Lex("->").Type);
        }

        [Test]
        public void LexEllipsis()
        {
            Lex("...", TokenType.ELLIPSIS, TokenType.EOF);
            Lex("..", TokenType.DOT, TokenType.DOT, TokenType.EOF);
        }

        [Test]
        public void Lex_Regression1()
        {
            Lex("'']", TokenType.STRING, TokenType.RBRACKET, TokenType.EOF);
        }

        [Test]
        public void Lex_LineExtension()
        {
            Lex("foo \\\r\n  bar", TokenType.ID, TokenType.ID, TokenType.EOF);
            Assert.AreEqual(2, lexer.LineNumber);
        }

        [Test]
        public void Lex_StrConstant()
        {
            Lex("\"\"\n", TokenType.STRING, TokenType.NEWLINE, TokenType.EOF);
        }

        [Test]
        public void Lex_LongInteger()
        {
            Lex("1L", TokenType.LONGINTEGER, TokenType.EOF);
        }

        [Test]
        public void Lex_LongZero()
        {
            Lex("0L", TokenType.LONGINTEGER, TokenType.EOF);
        }

        [Test]
        public void Lex_RawString()
        {
            var token = LexString(@"r'\''");
            Assert.AreEqual("\\'", token);
        }

        [Test]
        public void Lex_DecimalEscape()
        {
            var token = LexString(@"'\33'");
            Assert.AreEqual("\\33", token);
        }

        [Test]
        public void Lex_RealLiteral()
        {
            var token = Lex("0.1");
            Assert.AreEqual(TokenType.REAL, token.Type);
            Assert.AreEqual(0.1, token.Value);
        }

        [Test]
        public void Lex_UnicodeString()
        {
            var token = Lex("u'foo'");
            Assert.AreEqual(TokenType.STRING, token.Type);
            Assert.AreEqual("foo", ((Str)token.Value).s);
        }

        [Test]
        public void Lex_UnicodeStringConstant()
        {
            var token = Lex("u'\u00e9'");
            Assert.AreEqual(TokenType.STRING, token.Type);
            Assert.AreEqual("é", ((Str)token.Value).s);
        }

        [Test]
        public void Lex_Comment()
        {
            var token = Lex("# Hello\n");
            Assert.AreEqual(TokenType.COMMENT, token.Type);
            Assert.AreEqual(" Hello", token.Value);
        }

        [Test]
        public void Lex_Indented()
        {
            Lex("if x :\n    hi\n",
                TokenType.If, TokenType.ID, TokenType.COLON, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.ID, TokenType.NEWLINE, TokenType.DEDENT,
                TokenType.EOF);
        }

        [Test]
        public void Lex_IndentedComment()
        {
            Lex("if x :\n    #foo\n    hi\n",
                TokenType.If, TokenType.ID, TokenType.COLON, TokenType.NEWLINE,
                TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.ID, TokenType.NEWLINE,
                TokenType.DEDENT,
                TokenType.EOF);

        }

        [Test]
        public void Lex_UnevenComments()
        {
            Lex("#foo\n  #bar\n#baz\n",
                TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.COMMENT, TokenType.NEWLINE,
                TokenType.COMMENT, TokenType.NEWLINE, TokenType.EOF);
        }

        [Test]
        public void Lex_AddEq()
        {
            Lex("+=", TokenType.ADDEQ, TokenType.EOF);
        }

        [Test]
        public void Lex_StringWithCrLf()
        {
            var t = LexString("'\\r\\n'");
            Assert.AreEqual("\\r\\n", t);
        }

        [Test]
        public void Lex_BinaryString()
        {
            var t = Lex("b'\x00'");
            Assert.AreEqual(TokenType.STRING, t.Type);
            Assert.AreEqual("\x00", ((Bytes)t.Value).s);
        }

        [Test]
        public void Lex_Position()
        {
            var t = Lex("    x");
            Assert.AreEqual(0, t.Start);
            Assert.AreEqual(4, t.End);
            t = LexMore();
            Assert.AreEqual(4, t.Start);
            Assert.AreEqual(5, t.End);
        }

        [Test]
        public void Lex_PositionCrLf()
        {
            var t = Lex("\r\nx");
            Assert.AreEqual(2, t.Start, "Expected start at 2");
            Assert.AreEqual(3, t.End, "Expected end at 3");
        }

        [Test]
        public void Lex_Indent()
        {
            Lex("def foo():\n    return\n",
                TokenType.Def, TokenType.ID, TokenType.LPAREN, TokenType.RPAREN, TokenType.COLON, TokenType.NEWLINE,
                TokenType.INDENT, TokenType.Return, TokenType.NEWLINE, TokenType.DEDENT, TokenType.EOF);
        }

        [Test]
        public void Lex_Regression2()
        {
            Lex(@"r'\'', 'I'",
                TokenType.STRING, TokenType.COMMA, TokenType.STRING, TokenType.EOF);
        }
        
        [Test]
        public void Lex_FloatConstant()
        {
            Lex(".68",
                TokenType.REAL, TokenType.EOF);
        }

        [Test]
        public void Lex_Mixed_ByteStrings_Strings()
        {
            var tok = Lex("b\"bytes\" \"chars\"");
            Assert.IsAssignableFrom<Bytes>(tok.Value);
            tok = LexMore();
            Assert.IsAssignableFrom<Str>(tok.Value);
        }
    }
}
#endif
