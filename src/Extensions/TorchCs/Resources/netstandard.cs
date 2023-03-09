#region License
//  Copyright 2023 ToolGood
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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using static TorchSharp.torch;
using System.Linq;
using System.Text;

#pragma warning disable IDE1006 // 命名样式
#pragma warning disable CS8981 // 该类型名称仅包含小写 ascii 字符。此类名称可能会成为该语言的保留值。
#pragma warning disable CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。
namespace System
{
    public static partial class TorchExtension
    {
        public static string format(this string str, params object[] strings)
        {
            return String.Format(str, strings);
        }

        public static List<string> split(this string str, string splitStr)
        {
            return str.Split(splitStr).ToList();
        }
        public static string upper(this string str)
        {
            return str.ToUpper();
        }
        public static string lower(this string str)
        {
            return str.ToLower();
        }

        public static bool startswith(this string str, string s)
        {
            return str.StartsWith(s);
        }
        public static bool endswith(this string str, string s)
        {
            return str.EndsWith(s);
        }
        public static int find(this string str, string s, int start = 0)
        {
            return str.IndexOf(s, start);
        }
        public static int find(this string str, string s, int start, int end)
        {
            return str.IndexOf(s, start, end - start + 1);
        }
        public static int rfind(this string str, string s, int start = 0)
        {
            return str.LastIndexOf(s, start);
        }
        public static int rfind(this string str, string s, int start, int end)
        {
            return str.LastIndexOf(s, start, end - start + 1);
        }
        public static int index(this string str, string s, int start = 0)
        {
            var index = str.IndexOf(s, start);
            if (index == -1) throw new Exception("not find");
            return index;
        }
        public static int index(this string str, string s, int start, int end)
        {
            var index = str.IndexOf(s, start, end - start + 1);
            if (index == -1) throw new Exception("not find");
            return index;
        }
        public static int rindex(this string str, string s, int start = 0)
        {
            var index = str.LastIndexOf(s, start);
            if (index == -1) throw new Exception("not find");
            return index;
        }
        public static int rindex(this string str, string s, int start, int end)
        {
            var index = str.LastIndexOf(s, start, end - start + 1);
            if (index == -1) throw new Exception("not find");
            return index;
        }
        public static string replace(this string str, string oldStr, string newStr)
        {
            return str.Replace(oldStr, newStr);
        }
        public static string join(this string str, string[] objs)
        {
            return String.Join(str, objs);
        }
        public static string strip(this string str)
        {
            return str.Trim();
        }
        public static string lstrip(this string str)
        {
            return str.TrimStart();
        }
        public static string rstrip(this string str)
        {
            return str.TrimEnd();
        }

        public static T copy<T>(this T obj) where T : ICloneable
        {
            return (T)obj.Clone();
        }

        public static void append<T>(this ICollection<T> list, T obj)
        {
            list.Add(obj);
        }
        public static void remove<T>(this ICollection<T> list, T obj)
        {
            list.Remove(obj);
        }
        public static void extend<T>(this ICollection<T> list, params T[] objs)
        {
            foreach (var obj in objs) {
                list.Add(obj);
            }
        }
        public static int count<T>(this ICollection<T> list, T obj)
        {
            return list.Where(q => q.Equals(obj)).Count();
        }
        public static int index<T>(this ICollection<T> list, T obj)
        {
            var index = -1;
            foreach (var item in list) {
                index++;
                if (item.Equals(obj)) {
                    return index;
                }
            }
            return -1;
        }
        public static void reverse<T>(this ICollection<T> list)
        {
            list = list.Reverse().ToList();
        }
        public static void insert<T>(this IList<T> list, int index, T obj)
        {
            list.Insert(index, obj);
        }
        public static T pop<T>(this IList<T> list)
        {
            var last = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return last;
        }
        public static ICollection<T> copy<T>(this ICollection<T> list)
        {
            var newObj = new List<T>();
            newObj.AddRange(list);
            return newObj;
        }
        public static List<T> copy<T>(this List<T> list)
        {
            var newObj = new List<T>();
            newObj.AddRange(list);
            return newObj;
        }


        public static ICollection<T1> keys<T1, T2>(this IDictionary<T1, T2> dict)
        {
            return dict.Keys;
        }
        public static ICollection<T2> values<T1, T2>(this IDictionary<T1, T2> dict)
        {
            return dict.Values;
        }
        public static void clear<T1, T2>(this IDictionary<T1, T2> dict)
        {
            dict.Clear();
        }
        public static T2 get<T1, T2>(this IDictionary<T1, T2> dict, T1 key)
        {
            if (dict.TryGetValue(key, out T2 result)) {
                return result;
            }
            return default(T2);
        }
        public static T2 get<T1, T2>(this IDictionary<T1, T2> dict, T1 key, T2 def)
        {
            if (dict.TryGetValue(key, out T2 result)) {
                return result;
            }
            return def;
        }
        public static bool has_key<T1, T2>(this IDictionary<T1, T2> dict, T1 key)
        {
            return (dict.ContainsKey(key));
        }
        public static T2 pop<T1, T2>(this IDictionary<T1, T2> dict, T1 key)
        {
            if (dict.TryGetValue(key, out T2 result)) {
                dict.Remove(key);
                return result;
            }
            return default(T2);
        }
        public static T2 pop<T1, T2>(this IDictionary<T1, T2> dict, T1 key, T2 def)
        {
            if (dict.TryGetValue(key, out T2 result)) {
                dict.Remove(key);
                return result;
            }
            return def;
        }
        public static (T1, T2) popitem<T1, T2>(this IDictionary<T1, T2> dict)
        {
            T1 key = default(T1);
            T2 val = default(T2);
            foreach (var item in dict) {
                key = item.Key;
                val = item.Value;
            }
            if (dict.ContainsKey(key)) {
                dict.Remove(key);
            }
            return (key, val);
        }

        public static IDictionary<T1, T2> copy<T1, T2>(this IDictionary<T1, T2> dict)
        {
            Dictionary<T1, T2> copy = new Dictionary<T1, T2>();
            foreach (var item in dict) {
                copy[item.Key] = item.Value;
            }
            return copy;
        }
        public static Dictionary<T1, T2> copy<T1, T2>(this Dictionary<T1, T2> dict)
        {
            Dictionary<T1, T2> copy = new Dictionary<T1, T2>();
            foreach (var item in dict) {
                copy[item.Key] = item.Value;
            }
            return copy;
        }



        /// <summary>
        ///  Simplify code, similar to python syntax 
        ///  python code : B, L = queries.shape
        ///  csharp code : var (B, L) = queries.shape.ToLong2();
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static (long, long) ToLong2(this long[] array)
        {
            return (array[0], array[1]);
        }
        /// <summary>
        ///  Simplify code, similar to python syntax 
        ///  python code : B, L, _ = queries.shape
        ///  csharp code : var (B, L, _) = queries.shape.ToLong3();
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static (long, long, long) ToLong3(this long[] array)
        {
            return (array[0], array[1], array[2]);
        }
        /// <summary>
        ///  Simplify code, similar to python syntax 
        ///  python code : B, L, _, _ = queries.shape
        ///  csharp code : var (B, L, _, _) = queries.shape.ToLong4();
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static (long, long, long, long) ToLong4(this long[] array)
        {
            return (array[0], array[1], array[2], array[3]);
        }

    }

    public static partial class TorchEnumerable
    {
        public static IEnumerable<(TFirst First, TSecond Second)> zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            if (first is null) {
                throw new ArgumentNullException(nameof(first));
            }

            if (second is null) {
                throw new ArgumentNullException(nameof(second));
            }

            return ZipIterator(first, second);
        }

        public static IEnumerable<(TFirst First, TSecond Second, TThird Third)> zip<TFirst, TSecond, TThird>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third)
        {
            if (first is null) {
                throw new ArgumentNullException(nameof(first));
            }

            if (second is null) {
                throw new ArgumentNullException(nameof(second));
            }

            if (third is null) {
                throw new ArgumentNullException(nameof(third));
            }

            return ZipIterator(first, second, third);
        }

        private static IEnumerable<(TFirst First, TSecond Second)> ZipIterator<TFirst, TSecond>(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            using (IEnumerator<TFirst> e1 = first.GetEnumerator())
            using (IEnumerator<TSecond> e2 = second.GetEnumerator()) {
                while (e1.MoveNext() && e2.MoveNext()) {
                    yield return (e1.Current, e2.Current);
                }
            }
        }

        private static IEnumerable<(TFirst First, TSecond Second, TThird Third)> ZipIterator<TFirst, TSecond, TThird>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third)
        {
            using (IEnumerator<TFirst> e1 = first.GetEnumerator())
            using (IEnumerator<TSecond> e2 = second.GetEnumerator())
            using (IEnumerator<TThird> e3 = third.GetEnumerator()) {
                while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext()) {
                    yield return (e1.Current, e2.Current, e3.Current);
                }
            }
        }

    }

    public static class os
    {
        public static void makedirs(string path)
        {
            Directory.CreateDirectory(path);
        }
        public static void mkdir(string path)
        {
            Directory.CreateDirectory(path);
        }

        public static void rename(string oldPath, string newPath)
        {
            File.Move(oldPath, newPath);
        }
        public static void renames(string oldFolder, string newFolder)
        {
            Directory.Move(oldFolder, newFolder);
        }
        public static void remove(string path)
        {
            File.Delete(path);
        }
        public static void removedirs(string path)
        {
            Directory.Delete(path, true);
        }

        // chroot
        public static void chdir(string path)
        {
            Directory.SetCurrentDirectory(path);
        }
        public static string getcwd()
        {
            return Directory.GetCurrentDirectory();
        }

        public static void rmdir(string path)
        {
            Directory.Delete(path, true);
        }
        public static string tmpnam()
        {
            return Path.GetTempFileName();
        }
        public static string[] listdir(string path)
        {
            var fs = Directory.GetFiles(path);
            var dirs = Directory.GetDirectories(path);

            List<string> result = new List<string>();
            foreach (var f in fs) {
                result.Add(Path.GetFileName(f));
            }
            foreach (var dir in dirs) {
                result.Add(Path.GetFileName(dir));
            }
            return result.ToArray();
        }


        public class path
        {
            public static string join(params string[] paths)
            {
                var ps = paths.ToList();
                ps.RemoveAll(q => q == null);
                return Path.Combine(ps.ToArray());
            }
            public static bool exists(string path)
            {
                return File.Exists(path) || Directory.Exists(path);
            }
            public static bool isfile(string path)
            {
                return File.Exists(path);
            }
            public static bool isdir(string path)
            {
                return Directory.Exists(path);
            }
            public static string basename(string path)
            {
                return Path.GetFileName(path);
            }
            public static string abspath(string path)
            {
                return Path.GetFullPath(path);
            }

            public static string? dirname(string path)
            {
                return Path.GetDirectoryName(path);
            }

            public static long getsize(string path)
            {
                return new FileInfo(path).Length;
            }

            public static string[] split(string path)
            {
                var index = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                var s = path.Substring(0, index);
                var s2 = path.Substring(index + 1);
                return new string[] { s, s2 };
            }
            public static DateTime getmtime(string path)
            {
                return new FileInfo(path).LastWriteTime;
            }
            public static DateTime getctime(string path)
            {
                return new FileInfo(path).CreationTime;
            }
        }
    }
    public static class time
    {
        public static void sleep(int s)
        {
            Thread.Sleep(s * 1000);
        }
    }

    public class PythonFile
    {
        private System.IO.FileStream fileStream;
        private bool bin;

        public static PythonFile open(string file, string mode = "+", string encoding = "UTF-8")
        {
            PythonFile result = new PythonFile();

            if (mode.Contains("+"))
            {
                result.fileStream = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                if (mode.Contains("a"))
                {
                    result.fileStream.Seek(0, SeekOrigin.End);
                }
            } else if (mode.Contains("a"))
            {
                result.fileStream = File.Open(file, FileMode.OpenOrCreate, FileAccess.Write);
                result.fileStream.Seek(0, SeekOrigin.End);
            } else if (mode.Contains("w"))
            {
                result.fileStream = File.Open(file, FileMode.OpenOrCreate, FileAccess.Write);
            } else
            {
                result.fileStream = File.Open(file, FileMode.OpenOrCreate, FileAccess.Read);
            }
            result.bin = mode.Contains("b");
            return result;
        }
        public string[] readline(int size = 1)
        {
            var read = new System.IO.StreamReader(fileStream);
            string[] result = new string[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = read.ReadLine();
            }
            read.ReadToEnd();
            return result;
        }
        public string readline()
        {
            var read = new System.IO.StreamReader(fileStream);
            string result = read.ReadLine();
            read.ReadToEnd();
            return result;
        }
        public string read()
        {
            var read = new System.IO.StreamReader(fileStream);
            var r = read.Read();
            read.ReadToEnd();
            return ((char) r).ToString();
        }

        public string read(int size = 1)
        {
            if (size <= 0)
            {
                var read = new System.IO.StreamReader(fileStream);
                var r = read.ReadToEnd();
                read.ReadToEnd();
                return r;
            } else
            {
                var read = new System.IO.StreamReader(fileStream);
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < size; i++)
                {
                    var r = read.Read();
                    stringBuilder.Append((char) r);
                }
                read.ReadToEnd();
                return stringBuilder.ToString();
            }
        }

        public void write(string txt)
        {
            var write = new System.IO.StreamWriter(fileStream);
            write.Write(txt);
            write.Close();
        }

        public void write(double num)
        {
            if (bin)
            {
                var write = new System.IO.BinaryWriter(fileStream);
                write.Write(num);
                write.Close();
            } else
            {
                var write = new System.IO.StreamWriter(fileStream);
                write.Write(num.ToString());
                write.Close();
            }
        }
        public void write(float num)
        {
            if (bin)
            {
                var write = new System.IO.BinaryWriter(fileStream);
                write.Write(num);
                write.Close();
            } else
            {
                var write = new System.IO.StreamWriter(fileStream);
                write.Write(num.ToString());
                write.Close();
            }
        }
        public void write(int num)
        {
            if (bin)
            {
                var write = new System.IO.BinaryWriter(fileStream);
                write.Write(num);
                write.Close();
            } else
            {
                var write = new System.IO.StreamWriter(fileStream);
                write.Write(num.ToString());
                write.Close();
            }
        }
        public void write(long num)
        {
            if (bin)
            {
                var write = new System.IO.BinaryWriter(fileStream);
                write.Write(num);
                write.Close();
            } else
            {
                var write = new System.IO.StreamWriter(fileStream);
                write.Write(num.ToString());
                write.Close();
            }
        }

        public void seek(int offset, int whence = 0)
        {
            if (whence == 0)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
            } else if (whence == 1)
            {
                fileStream.Seek(offset, SeekOrigin.Current);
            } else if (whence == 2)
            {
                fileStream.Seek(offset, SeekOrigin.End);
            } else
            {
                throw new Exception("whence is error.");
            }
        }

        public long tell()
        {
            return fileStream.Position;
        }

        public void close()
        {
            fileStream.Close();
        }
    }

}
#pragma warning restore CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。
#pragma warning restore CS8981 // 该类型名称仅包含小写 ascii 字符。此类名称可能会成为该语言的保留值。
#pragma warning restore IDE1006 // 命名样式