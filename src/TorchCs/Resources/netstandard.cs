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

        public static (long, long) ToLong2(this long[] array)
        {
            return (array[0], array[1]);
        }
        public static (long, long, long) ToLong3(this long[] array)
        {
            return (array[0], array[1], array[2]);
        }
        public static (long, long, long, long) ToLong4(this long[] array)
        {
            return (array[0], array[1], array[2], array[3]);
        }


        public static long[] ToArray(this (long, long, long, long) temp)
        {
            return new long[] { temp.Item1, temp.Item2, temp.Item3, temp.Item4 };
        }
        public static long[] ToArray(this (long, long, long) temp)
        {
            return new long[] { temp.Item1, temp.Item2, temp.Item3 };
        }
        public static long[] ToArray(this (long, long) temp)
        {
            return new long[] { temp.Item1, temp.Item2 };
        }
        public static long[] ToArray(this long temp)
        {
            return new long[] { temp };
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
            internal const char DirectorySeparatorChar = '\\'; // Windows implementation
            internal const char AltDirectorySeparatorChar = '/';
            internal const string DirectorySeparatorCharAsString = "\\";

            public static string join(string path1, string path2)
            {
                if (path1 is null || path1.Length == 0)
                    return path2;
                if (path2 is null || path2.Length == 0)
                    return path1;

                bool hasSeparator = IsDirectorySeparator(path1[path1.Length - 1]) || IsDirectorySeparator(path2[0]);
                return hasSeparator ? string.Concat(path1, path2) : string.Concat(path1, DirectorySeparatorCharAsString, path2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool IsDirectorySeparator(char c) => c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;

            public static string join(string path1, string path2, string path3)
            {
                if (path1 is null || path1.Length == 0)
                    return join(path2, path3);

                if (path2 is null || path2.Length == 0)
                    return join(path1, path3);

                if (path3 is null || path3.Length == 0)
                    return join(path1, path2);

                bool firstHasSeparator = IsDirectorySeparator(path1[path1.Length - 1]) || IsDirectorySeparator(path2[0]);
                bool secondHasSeparator = IsDirectorySeparator(path2[path2.Length - 1]) || IsDirectorySeparator(path3[0]);
                return path1 + (firstHasSeparator ? "" : DirectorySeparatorCharAsString) + path2 + (secondHasSeparator ? "" : DirectorySeparatorCharAsString) + path3;
            }

            public static string join(string path1, string path2, string path3, string path4)
            {
                if (path1 is null || path1.Length == 0)
                    return join(path2, path3, path4);

                if (path2 is null || path2.Length == 0)
                    return join(path1, path3, path4);

                if (path3 is null || path3.Length == 0)
                    return join(path1, path2, path4);

                if (path4 is null || path4.Length == 0)
                    return join(path1, path2, path3);

                bool firstHasSeparator = IsDirectorySeparator(path1[path1.Length - 1]) || IsDirectorySeparator(path2[0]);
                bool secondHasSeparator = IsDirectorySeparator(path2[path2.Length - 1]) || IsDirectorySeparator(path3[0]);
                bool thirdHasSeparator = IsDirectorySeparator(path3[path3.Length - 1]) || IsDirectorySeparator(path4[0]);

                return path1 + (firstHasSeparator ? "" : DirectorySeparatorCharAsString) +
                    path2 + (secondHasSeparator ? "" : DirectorySeparatorCharAsString) +
                    path3 + (thirdHasSeparator ? "" : DirectorySeparatorCharAsString) +
                    path4;
            }

            public static bool exists(string path)
            {
                if (File.Exists(path)) {
                    return true;
                }
                if (Directory.Exists(path)) {
                    return true;
                }
                return false;
            }
        }
    }
}
