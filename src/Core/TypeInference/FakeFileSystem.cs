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

#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.TypeInference
{
    public class FakeFileSystem : IFileSystem
    {
        private FakeDirectory root;
        private FakeDirectory dir;

        public FakeFileSystem()
        {
            this.root = new FakeDirectory();
            this.dir = root;
        }

        public class Entry 
        {
            public FakeDirectory Parent;
        }

        public class FakeFile : Entry
        {
            public string Contents;
        }

        public class FakeDirectory : Entry
        {
            public Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        }

        public FakeFileSystem File(string name, params string[] lines)
        {
            dir.Entries[name] = new FakeFile
            {
                Contents = string.Join("\r\n", lines),
                Parent = dir
            };
            return this;
        }

        public FakeFileSystem Dir(string name)
        {
            var d = new FakeDirectory
            {
                Parent = dir
            };
            dir.Entries[name] = d;
            dir = d;
            return this;
        }

        public FakeFileSystem End()
        {
            dir = dir.Parent;
            return this;
        }

        public string DirectorySeparatorChar
        {
            get { return "\\"; }
        }

        private FakeDirectory Traverse(IEnumerable<string> segs)
        {
            var d = root;
            foreach (var seg in segs)
            {
                Entry e;
                if (!d.Entries.TryGetValue(seg, out e))
                    return null;
                d = (FakeDirectory) e;
            }
            return d;
        }

        public void CreateDirectory(string path)
        {
            var segs = path.Split(new [] {this.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries);
            var d = root;
            foreach (var seg in segs)
            {
                Entry e;
                if (!d.Entries.TryGetValue(seg, out e))
                {
                    var dNew = new FakeDirectory
                    {
                        Parent = d
                    };
                    d.Entries.Add(seg, dNew);
                    d = dNew;
                }
                else
                {
                    d = (FakeDirectory) e;
                }
            }
        }

        public TextReader CreateStreamReader(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FakeDirectory d = Traverse(segs.Take(segs.Length - 1));
            Debug.Assert(d != null, path);
            Debug.Assert(d.Entries != null, "d.entries");
            Debug.Assert(segs != null, "segs");
            return new StringReader(((FakeFile)d.Entries[segs.Last()]).Contents);
        }

        public void DeleteFile(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FakeDirectory d = Traverse(segs.Take(segs.Length - 1));
            d.Entries.Remove(segs.Last());
        }

        public void DeleteDirectory(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FakeDirectory d = Traverse(segs.Take(segs.Length - 1));
            d.Entries.Remove(segs.Last());
        }

        public string CombinePath(string dir, string file)
        {
            return string.Join(DirectorySeparatorChar, dir, file);
        }

        public bool DirectoryExists(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FakeDirectory d = Traverse(segs.Take(segs.Length - 1));
            if (d == null)
                return false;
            Entry e;
            if (!d.Entries.TryGetValue(segs.Last(), out e))
                return false;
            return e is FakeDirectory;
        }

        public bool FileExists(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FakeDirectory d = Traverse(segs.Take(segs.Length - 1));
            if (d == null)
                return false;
            Entry e;
            if (!d.Entries.TryGetValue(segs.Last(), out e))
                return false;
            return e is FakeFile;
        }

        public string GetDirectoryName(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            int i = path.IndexOf(DirectorySeparatorChar);
            if (i <= 0)
                return "";
            return path.Remove(i);
        }

        public string getFileHash(string path)
        {
            return ReadFile(path).GetHashCode().ToString();
        }

        public IEnumerable<string> GetDirectories(string path, string pattern, SearchOption option)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<string> GetFiles(string path, string pattern, SearchOption option)
        {
            throw new NotImplementedException();
        }

        public string[] GetFileSystemEntries(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var d = Traverse(segs);
            return d.Entries.Keys
                .Select(name => path + DirectorySeparatorChar + name)
                .ToArray();
        }

        public string getSystemTempDir()
        {
            return "\\tmp";
        }

        public string GetFileName(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            return segs.Last();
        }

        public string GetFileNameWithoutExtension(string path)
        {
            var filename = GetFileName(path);
            int i = filename.LastIndexOf('.');
            if (i > 0)
            {
                filename = filename.Remove(i);
            }
            return filename;
        }

        public string makePathString(params string[] files)
        {
            return string.Join(DirectorySeparatorChar, files);
        }

        public byte[] ReadFileBytes(string path)
        {
            return Encoding.UTF8.GetBytes(ReadFile(path));
        }

        public string ReadFile(string path)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FakeDirectory d = Traverse(segs.Take(segs.Length - 1));
            return ((FakeFile) d.Entries[segs.Last()]).Contents;
        }

        public string relPath(string path1, string path2)
        {
            throw new NotImplementedException();
        }

        public string GetFullPath(string file)
        {
            return file;
        }

        public void WriteFile(string path, string contents)
        {
            var segs = path.Split(new[] { this.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            FakeDirectory d = Traverse(segs.Take(segs.Length - 1));
            ((FakeFile) d.Entries[segs.Last()]).Contents = contents;
        }

        public TextWriter CreateStreamWriter(Stream stm, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public Stream CreateFileStream(string outputFileName, FileMode mode, FileAccess access)
        {
            throw new NotImplementedException();
        }
    }
}
#endif