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


        public static void append<T>(this ICollection<T> list, T obj)
        {
            list.Add(obj);
        }

        public static ICollection<T1> keys<T1, T2>(this IDictionary<T1, T2> dict)
        {
            return dict.Keys;
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



}
