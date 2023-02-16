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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pytocs.runtime
{
    public static class @struct
    {
        public static T unpack<T>(string format, byte[] buffer)
        {
            bool littleEndian = BitConverter.IsLittleEndian;
            int c = 0;
            var oTuple = new List<object>();
            var i = 0;
            foreach (var ch in format)
            {
                switch (ch)
                {
                case '<': littleEndian = true; break;
                case '>': littleEndian = false; break;
                case 'x': i += Count(c); c = 0;  break; // skip padding.
                case 'i': oTuple.Add(ReadInt32(littleEndian, buffer, ref i)); c = 0;  break;
                case 'I': oTuple.Add((uint)ReadInt32(littleEndian, buffer, ref i)); c = 0; break;
                case 'h': oTuple.Add(ReadInt16(littleEndian, buffer, ref i)); c = 0; break;
                case 'H': oTuple.Add((ushort)ReadInt16(littleEndian, buffer, ref i)); c = 0; break;
                case 's': oTuple.Add(ReadString(Count(c), buffer,  ref i)); c = 0; break;
                default:
                    if (char.IsDigit(ch))
                    {
                        c = c * 10 + (ch - '0');
                        break;
                    }
                    throw new ArgumentException($"Unsupported format character '{ch}'.");
                }
            }
            return (T)Activator.CreateInstance(typeof(T), oTuple.ToArray())!;
        }

        private static int Count(int n)
        {
            return n != 0 ? n : 1;
        }

        private static int ReadInt32(bool littleEndian, byte[] buffer, ref int i)
        {
            if (littleEndian)
                return ReadLeInt32(buffer, ref i);
            else
                return ReadBeInt32(buffer, ref i);
        }

        private static int ReadInt16(bool littleEndian, byte[] buffer, ref int i)
        {
            if (littleEndian)
                return ReadLeInt16(buffer, ref i);
            else
                return ReadBeInt16(buffer, ref i);
        }

        private static short ReadLeInt16(byte[] buffer, ref int i)
        {
            if (i + 2 > buffer.Length)
                throw new InvalidOperationException();
            int n = buffer[i] |
                    buffer[i + 1] << 8;
            i += 2;
            return (short)n;
        }

        private static short ReadBeInt16(byte[] buffer, ref int i)
        {
            if (i + 2 > buffer.Length)
                throw new InvalidOperationException();
            int n = buffer[i] << 8 |
                    buffer[i + 1];
            i += 2;
            return (short)n;
        }

        private static int ReadLeInt32(byte[] buffer, ref int i)
        {
            if (i + 4 > buffer.Length)
                throw new InvalidOperationException();
            int n = buffer[i] |
                    buffer[i + 1] << 8 |
                    buffer[i + 2] << 16 |
                    buffer[i + 3] << 24;
            i += 4;
            return n;
        }

        private static int ReadBeInt32(byte[] buffer, ref int i)
        {
            if (i + 4 > buffer.Length)
                throw new InvalidOperationException();
            int n = buffer[i]  << 24 |
                    buffer[i + 1] << 16 |
                    buffer[i + 2] << 8 |
                    buffer[i + 3];
            i += 4;
            return n;
        }

        private static string ReadString(int count, byte[] buffer, ref int i)
        {
            //$TODO: what encoding? or should it be byte[]?
            var s = Encoding.UTF8.GetString(buffer, i, count);
            i += count;
            return s;
        }
    }
}
