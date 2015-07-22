using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class IndentingTextWriter
    {
        private TextWriter writer;
        private bool atStartOfLine;

        private static HashSet<string> keywords = new HashSet<string>
        {
            "base",
            "bool",
            "const",
            "case",
            "decimal",
            "default",
            "event",
            "false",
            "int",
            "new",
            "true",
            "sizeof",
            "static",
            "string",
            "struct",
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
            return keywords.Contains(name);
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
