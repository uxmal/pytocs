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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.TypeInference
{
    public interface IFileSystem
    {
        void CreateDirectory(string f);
        TextReader CreateStreamReader(string filename);
        void DeleteDirectory(string directory);
        string CombinePath(string dir, string file);
        bool DirectoryExists(string filePath);
        bool FileExists(string filePath);
        string? GetDirectoryName(string filePath);
        string getFileHash(string path);
        string [] GetFileSystemEntries(string file_or_dir);
        string GetTempPath();
        string GetFileName(string path);
        string makePathString(params string[] files);
        byte[] ReadFileBytes(string path);
        string? ReadFile(string path);
        string GetFullPath(string file);
        void WriteFile(string path, string contents);
        void DeleteFile(string path);
        TextWriter CreateStreamWriter(Stream stm, Encoding encoding);
        Stream CreateFileStream(string outputFileName, FileMode mode, FileAccess access);
        IEnumerable<string> GetDirectories(string directoryName, string v, SearchOption option);
        IEnumerable<string> GetFiles(string directoryName, string v, SearchOption option);
        string GetFileNameWithoutExtension(string file);
    }

    public class FileSystem : IFileSystem
    {
        public void CreateDirectory(string directory) { Directory.CreateDirectory(directory); }

        public TextReader CreateStreamReader(string filename) { return new StreamReader(filename); }

        public void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                string[] files = Directory.GetFileSystemEntries(directory);
                if (files != null)
                {
                    foreach (string f in files)
                    {
                        if (Directory.Exists(f))
                        {
                            DeleteDirectory(f);
                        }
                        else
                        {
                            File.Delete(f);
                        }
                    }
                }
                Directory.Delete(directory);
            }
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public string CombinePath(string dir, string file)
        {
            return Path.Combine(Path.GetFullPath(dir), file);
        }

        public bool DirectoryExists(string dirPath) { return Directory.Exists(dirPath); }

        public bool FileExists(string filePath) { return File.Exists(filePath); }

        public string GetTempPath()
        {
            return Path.GetTempPath();
            }

        public IEnumerable<string> GetDirectories(string directory, string searchPattern, SearchOption options)
        {
            return Directory.GetDirectories(directory, searchPattern, options);
        }

        public IEnumerable<string> GetFiles(string directory, string searchPattern, SearchOption options)
        {
            return Directory.GetFiles(directory, searchPattern, options);
        }

        public string? GetDirectoryName(string filePath) { return Path.GetDirectoryName(filePath); }
        public string GetFileName(string filePath) { return Path.GetFileName(filePath); }
        public string GetFileNameWithoutExtension(string filePath) { return Path.GetFileNameWithoutExtension(filePath); }

        public string[] GetFileSystemEntries(string dirPath) { return Directory.GetFileSystemEntries(dirPath); } 
        
        public string makePathString(params string[] files)
        {
            return Path.Combine(files);
        }

        public string getFileHash(string path)
        {
            byte[] bytes = ReadFileBytes(path);
            return getContentHash(Encoding.UTF8.GetBytes(path)) + "." + getContentHash(bytes);
        }

        public static string getContentHash(byte[] fileContents)
        {
            HashAlgorithm algorithm = HashAlgorithm.Create("SHA1")!;
            byte[] messageDigest = algorithm.ComputeHash(fileContents);
            var sb = new StringBuilder();
            foreach (byte aMessageDigest in messageDigest)
            {
                sb.Append(string.Format("{0:X2}", 0xFF & aMessageDigest));
            }
            return sb.ToString();
        }

        public string? ReadFile(string path)
        {
            // Don't use line-oriented file read -- need to retain CRLF if present
            // so the style-run and link offsets are correct.
            byte[] content;
            try
            {
                content = File.ReadAllBytes(path);
                return Encoding.UTF8.GetString(content);
            }
            catch
            {
                return null;
            }
        }

        public byte[] ReadFileBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public Stream CreateFileStream(string filename, FileMode mode, FileAccess access)
        {
            return new FileStream(filename, mode, access);
        }

        public TextWriter CreateStreamWriter(Stream stm, Encoding encoding)
        {
            return new StreamWriter(stm, encoding);
        }
        
        public string GetFullPath(string file)
        {
            return Path.GetFullPath(file);
        }

        public void WriteFile(string path, string contents)
        {
            using (TextWriter output = new StreamWriter(path))
            {
                output.Write(contents);
                output.Flush();
            }
        }
    }
}
