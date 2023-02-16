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

namespace System
{
    public static partial class TorchExtension
    {
        public static string format(this string str, params object[] strings)
        {
            return String.Format(str, strings);
        }

        public static string[] split(this string str, string splitStr)
        {
            return str.Split(splitStr);
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
        }
    }
}
