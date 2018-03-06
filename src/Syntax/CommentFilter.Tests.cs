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
    public class CommentFilterTests
    {
        private CommentFilter comfil;

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
                if (encodedTokens.Length <= i)
                {
                    Assert.Fail($"Unexpected {tok}");
                    return;
                }
                switch (encodedTokens[i])
                {
                case 'i': Assert.AreEqual(TokenType.ID, tok.Type); break;
                case 'N': Assert.AreEqual(TokenType.NEWLINE, tok.Type); break;
                case 'I': Assert.AreEqual(TokenType.INDENT, tok.Type); break;
                case 'D': Assert.AreEqual(TokenType.DEDENT, tok.Type); break;
                case '#': Assert.AreEqual(TokenType.COMMENT, tok.Type); break;
                default: Assert.Fail($"Unexpected {tok.Type}"); break;
                }
                ++i;
            }
            Assert.AreEqual(encodedTokens.Length, i, $"Expected {encodedTokens.Length} tokens");
        }

        [Test]
        public void Comfil_Simple()
        {
            var pySrc = "hello\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iN");
        }

        [Test]
        public void Comfil_Indent()
        {
            var pySrc = "hello\n    hello\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iNIiND");
        }

        [Test]
        public void Comfil_Comment()
        {
            var pySrc = "hello\n# hello\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iN#N");
        }

        [Test]
        public void Comfil_IndentedComment()
        {
            var pySrc = "hello\n    #hello\nbye\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iN#NiN");
        }

        [Test]
        public void Comfil_DedentedComment()
        {
            var pySrc = "one\n  two\n  #dedent\nthree\n";
            Create_CommentFilter(pySrc);
            AssertTokens("iNIiN#NDiN");
        }
    }
}
#endif
