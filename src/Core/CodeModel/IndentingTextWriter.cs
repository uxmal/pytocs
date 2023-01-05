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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.CodeModel
{
    public class IndentingTextWriter
    {
        private TextWriter writer;
        private bool atStartOfLine;

        private static HashSet<string> csharpKeywords = new HashSet<string>
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "volatile",
            "while",
        };

        public IndentingTextWriter(TextWriter writer)
        {
            this.writer = writer;
            this.atStartOfLine = true;
        }

        public int IndentLevel { get; set; }

        public void Write(string s)
        {
            EnsureIndentation();
            this.writer.Write(s);
        }

        internal void WriteLine()
        {
            EnsureIndentation();
            writer.WriteLine();
            atStartOfLine = true;
        }

        internal void WriteLine(string str)
        {
            EnsureIndentation();
            writer.WriteLine(str);
            atStartOfLine = true;
        }

        public void WriteName(string name)
        {
            EnsureIndentation();
            if (NameNeedsQuoting(name))
                writer.Write("@");
            writer.Write(name);
        }

        public static string QuoteName(string name)
        {
            if (NameNeedsQuoting(name))
                return "@" + name;
            else
                return name;
        }

        public static bool NameNeedsQuoting(string name)
        {
            if (name.Contains("__"))
                return true;
            return csharpKeywords.Contains(name);
        }

        internal void Write(string format, params object [] args)
        {
            EnsureIndentation();
            writer.Write(format, args);
        }

        private void EnsureIndentation()
        {
            if (atStartOfLine)
            {
                writer.Write(new string(' ', 4 * IndentLevel));
                atStartOfLine = false;
            }
        }

        public void WriteDottedName(string dottedString)
        {
            var sep = false;
            foreach (var name in dottedString.Split('.'))
            {
                if (sep) writer.Write('.');
                sep = true;
                WriteName(name);
            }
        }

        public void Write(char ch)
        {
            EnsureIndentation();
            writer.Write(ch);
        }
    }
}
