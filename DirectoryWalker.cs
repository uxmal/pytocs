using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs
{
    public class DirectoryWalker
    {
        private string rootDirectory;
        private string pattern;

        public DirectoryWalker(string pattern) : this(Directory.GetCurrentDirectory(), pattern)
        {
        }

        public DirectoryWalker(string directory, string pattern)
        {
            this.rootDirectory = directory;
            this.pattern = pattern;
        }

        public class EnumerationState
        {
            public string DirectoryName;
            public string Namespace;
        }

        public void Enumerate()
        {
            var stack = new Stack<IEnumerator<EnumerationState>>();
            stack.Push(new List<EnumerationState>{new EnumerationState 
            {
                DirectoryName = rootDirectory,
                Namespace = ""
            }}.GetEnumerator());
            while (stack.Count > 0)
            {
                var e = stack.Pop();
                if (!e.MoveNext())
                    continue;
                stack.Push(e);
                var state = e.Current;
                ProcessDirectoryFiles(state);
                e = (Directory.GetDirectories(state.DirectoryName, "*", SearchOption.TopDirectoryOnly)
                     .Select(d => new EnumerationState
                     {
                         DirectoryName = d,
                         Namespace = GenerateNamespace(state, d),
                     })).GetEnumerator();
                stack.Push(e);
            }
        }

        private static string GenerateNamespace(EnumerationState state, string dirname)
        {
            dirname = Path.GetFileName(dirname)
                .Replace('-', '_')
                .Replace('.', '_');
            return string.Format(
                state.Namespace.Length > 0 ? "{0}.{1}" : "{1}",
                state.Namespace,
                dirname);
        }

        public void ProcessDirectoryFiles(EnumerationState state)
        {
            foreach (var file in Directory.GetFiles(state.DirectoryName, "*.py", SearchOption.TopDirectoryOnly))
            {
                var xlator = new Translator(state.Namespace, Path.GetFileNameWithoutExtension(file));
                xlator.TranslateFile(file, file + ".cs");
            }
        }
    }
}
