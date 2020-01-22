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

namespace Pytocs.Core.Syntax
{
    public struct Token
    {
        public readonly int LineNumber;
        public readonly int Indent;
        public readonly TokenType Type;
        public readonly object Value;
        public readonly object NumericValue;
        public readonly int Start;
        public readonly int End;

        public Token(int lineNumber, int indent, TokenType type, object value, object numericValue, int start, int end)
        {
            LineNumber = lineNumber;
            Indent = indent;
            Type = type;
            Value = value;
            NumericValue = numericValue;
            Start = start;
            End = end;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is Token)
            {
                Token oToken = (Token)obj;
                return this == oToken;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = Type.GetHashCode();
            if (Value == null)
            {
                return h;
            }

            return (h * 17) | Value.GetHashCode();
        }

        public static bool operator ==(Token a, Token b)
        {
            return a.Type == b.Type && Equals(a.Value, b.Value);
        }

        public static bool operator !=(Token a, Token b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format(
                Value != null ? "{{{0} ({1})}}" : "{{{0}}}",
                Type,
                Value);
        }
    }
}