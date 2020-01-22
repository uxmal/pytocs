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

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Pytocs.Core.TypeInference
{
    public interface IFileSystem
    {
        string DirectorySeparatorChar { get; }

        void CreateDirectory(string f);

        TextReader CreateStreamReader(string filename);

        void DeleteDirectory(string directory);

        string CombinePath(string dir, string file);

        bool DirectoryExists(string filePath);

        bool FileExists(string filePath);

        string GetDirectoryName(string filePath);

        string getFileHash(string path);

        string[] GetFileSystemEntries(string file_or_dir);

        string getSystemTempDir();

        string GetFileName(string path);

        string makePathString(params string[] files);

        byte[] ReadFileBytes(string path);

        string ReadFile(string path);

        string relPath(string path1, string path2);

        string GetFullPath(string file);

        void WriteFile(string path, string contents);

        void DeleteFile(string path);

        TextWriter CreateStreamWriter(Stream stm, Encoding encoding);

        Stream CreateFileStream(string outputFileName, FileMode mode, FileAccess access);

        IEnumerable<string> GetDirectories(string directoryName, string v, SearchOption option);

        IEnumerable<string> GetFiles(string directoryName, string v, SearchOption option);

        string GetFileNameWithoutExtension(string file);
    }
}