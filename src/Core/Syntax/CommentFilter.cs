#region License

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pytocs.Core.Syntax
{
    /// <summary>
    ///     Lexer post processor that deals with comments.
    /// </summary>
    /// <remarks>
    ///     Python's significant whitespace causes headaches when dealing
    ///     with comments. Is an indented comment supposed to cause an indented code
    ///     block?
    /// </remarks>
    public class CommentFilter : ILexer
    {
        private int iHead;
        private readonly ILexer lexer;
        private TokenType prevTokenType;
        private readonly List<Token> queue;
        private Token token;

        public CommentFilter(ILexer lexer)
        {
            this.lexer = lexer;
            queue = new List<Token>();
            iHead = 0;
            prevTokenType = TokenType.NONE;
        }

        public int LineNumber => lexer.LineNumber;

        public Token Get()
        {
            if (token.Type != TokenType.NONE)
            {
                Token t = token;
                token = new Token();
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

        private Token GetToken()
        {
            if (iHead < queue.Count)
            {
                prevTokenType = queue[iHead].Type;
                return queue[iHead++];
            }

            iHead = 0;
            queue.Clear();

            Token tok = lexer.Get();
            if (tok.Type != TokenType.COMMENT ||
                prevTokenType != TokenType.NEWLINE &&
                prevTokenType != TokenType.INDENT &&
                prevTokenType != TokenType.DEDENT)
            {
                prevTokenType = tok.Type;
                return tok;
            }

            // Start reading comment lines.
            List<Line> lines = new List<Line>();
            Line line = ReadLine(tok);
            lines.Add(line);
            Debug.Assert(line.IsComment());
            do
            {
                line = ReadLine();
                lines.Add(line);
            } while (line.IsComment());

            if (lines.Count == 1)
            {
                queue.AddRange(lines[0].Tokens);
                return queue[iHead++];
            }

            // We have a bundle of > 1 lines that ends with a non-comment line.
            // The first one was indented or dedented. If the last, non-comment, line
            // is indented or dedented, all previous lines must also be in- or
            // dedented. If the last line is not dedented.
            Line lastLine = lines.Last();
            switch (lastLine.Tokens[0].Type)
            {
                case TokenType.INDENT:
                    queue.AddRange(lines.SelectMany(l => l.Tokens));
                    break;

                case TokenType.DEDENT:
                    if (lines[0].Tokens[0].Indent == lastLine.Tokens[0].Indent)
                    {
                        // IF the preceding comment's indentation was the same
                        // as that of the dedent, we want to move the dedent.
                        List<Token> dedent = lastLine.RemoveFirst(TokenType.DEDENT);
                        lines[0].Tokens.InsertRange(0, dedent);
                    }

                    queue.AddRange(lines.SelectMany(l => l.Tokens));
                    break;

                default:
                    queue.AddRange(lines.SelectMany(l => l.Tokens));
                    break;
            }

            prevTokenType = queue[iHead].Type;
            return queue[iHead++];
        }

        private Line ReadLine(Token tok)
        {
            List<Token> list = new List<Token> { tok };
            do
            {
                tok = lexer.Get();
                list.Add(tok);
            } while (tok.Type != TokenType.EOF && tok.Type != TokenType.NEWLINE);

            return new Line { Tokens = list };
        }

        private Line ReadLine()
        {
            List<Token> list = new List<Token>();
            Token tok;
            do
            {
                tok = lexer.Get();
                list.Add(tok);
            } while (tok.Type != TokenType.EOF && tok.Type != TokenType.NEWLINE);

            return new Line { Tokens = list };
        }

        private class Line
        {
            public List<Token> Tokens;
            public TokenType Type => Tokens[0].Type;

            internal bool IsComment()
            {
                int i = 0;
                if (Tokens[i].Type == TokenType.INDENT ||
                    Tokens[i].Type == TokenType.DEDENT)
                {
                    ++i;
                }

                return
                    Tokens[i].Type == TokenType.COMMENT ||
                    Tokens[i].Type == TokenType.NEWLINE;

                throw new NotImplementedException();
            }

            public List<Token> RemoveFirst(TokenType type)
            {
                int iStart = Tokens.FindIndex(t => t.Type == type);
                if (iStart < 0)
                {
                    return new List<Token>();
                }

                int iEnd = Tokens.FindIndex(iStart, t => t.Type != type);
                if (iEnd < 0)
                {
                    iEnd = Tokens.Count;
                }

                List<Token> range = Tokens.GetRange(iStart, iEnd - iStart);
                Tokens.RemoveRange(iStart, iEnd - iStart);
                Debug.Assert(range.All(t => t.Type == type));
                return range;
            }

            public void ReplaceEof()
            {
                for (int i = 0; i < Tokens.Count; ++i)
                {
                    Token t = Tokens[i];
                    if (t.Type == TokenType.EOF)
                    {
                        Tokens[i] = new Token(
                            t.LineNumber, t.Indent,
                            TokenType.NEWLINE, null, null,
                            t.Start, t.End);
                    }
                }
            }
        }
    }
}