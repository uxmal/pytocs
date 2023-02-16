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

namespace Pytocs.UnitTests.Syntax
{
    public class CommentFilterTests
    {
        private CommentFilter comfil = default!;

        private void Create_CommentFilter(string pySrc)
        {
            this.comfil = new CommentFilter(new Lexer("test.py", new StringReader(pySrc)));
        }

        private void AssertTokens(string encodedTokens)
        {
            int i = 0;
            for (; ; )
            {
                var tok = comfil.Get();
                if (tok.Type == TokenType.EOF)
                    break;
                Assert.True(encodedTokens.Length > i, $"Unexpected {tok}");
                switch (encodedTokens[i])
                {
                case '(': Assert.Equal(TokenType.LPAREN, tok.Type); break;
                case ')': Assert.Equal(TokenType.RPAREN, tok.Type); break;
                case ':': Assert.Equal(TokenType.COLON, tok.Type); break;
                case 'd': Assert.Equal(TokenType.Def, tok.Type); break;
                case 'i': Assert.Equal(TokenType.ID, tok.Type); break;
                case 'N': Assert.Equal(TokenType.NEWLINE, tok.Type); break;
                case 'I': Assert.Equal(TokenType.INDENT, tok.Type); break;
                case 'D': Assert.Equal(TokenType.DEDENT, tok.Type); break;
                case '#': Assert.Equal(TokenType.COMMENT, tok.Type); break;
                default: throw new InvalidOperationException($"Unexpected {tok.Type}");
                }
                ++i;
            }
            Assert.Equal(encodedTokens.Length, i); // Expected {encodedTokens.Length} tokens
        }

        [Fact]
        public void Comfil_Simple()
        {
            var pySrc = "hello\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iN");
        }

        [Fact]
        public void Comfil_Indent()
        {
            var pySrc = "hello\n    hello\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iNIiND");
        }

        [Fact]
        public void Comfil_Comment()
        {
            var pySrc = "hello\n# hello\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iN#N");
        }

        [Fact]
        public void Comfil_IndentedComment()
        {
            var pySrc = "hello\n    #hello\nbye\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iN#NiN");
        }

        [Fact]
        public void Comfil_DedentedComment()
        {
            var pySrc = "one\n  two\n  #dedent\nthree\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iNIiN#NDiN");
        }

        [Fact]
        public void Comfil_DedentAfterDef()
        {
            var pySrc = "def f():\n    id\n\n# comment\ndef g():\n";
            Create_CommentFilter(pySrc);
            AssertTokens("di():NIiND#Ndi():N");
        }
    }
}
#endif
