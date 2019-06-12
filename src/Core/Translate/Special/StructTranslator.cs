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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate.Special
{
    public class StructTranslator
    {
        public CodeExpression? Translate(CodeGenerator m, CodeFieldReferenceExpression method, CodeExpression[] args)
        {
            if (method.FieldName == "unpack")
            {
                return TranslateUnpack(args);
            }
            return null;
        }

        public CodeExpression? TranslateUnpack(CodeExpression[]args)
        {
            if (args == null || args.Length < 2)
                return null;
            var e = CreateFormatEnumerator(args[0]);
            if (e == null)
                return null;
            var littleEndian = BitConverter.IsLittleEndian;
            int count = 0;
            foreach (var ch in e)
            {
                switch (ch)
                {
                case '<': count = 0; littleEndian = true; break;
                case '>': count = 0; littleEndian = false; break;
                case 'x': count = 0; break;// discard padding 
                default:
                    if (char.IsDigit(ch))
                    {
                        count = count * 10 + (ch - '0');
                    }
                    break;
                }
            }
            return null;
        }

        private IEnumerable<char>? CreateFormatEnumerator(CodeExpression formatString)
        {
            if (formatString is CodePrimitiveExpression p && 
                p.Value is Str s)
            {
                return s.Value;
            }
            return null;
        }
    }
}
