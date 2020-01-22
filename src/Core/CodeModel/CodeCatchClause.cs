﻿#region License

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

namespace Pytocs.Core.CodeModel
{
    public class CodeCatchClause
    {
        public CodeCatchClause()
        {
            Statements = new List<CodeStatement>();
        }

        public CodeCatchClause(string localName) : this()
        {
            LocalName = localName;
        }

        public CodeCatchClause(string localName, CodeTypeReference catchExceptionType) : this()
        {
            LocalName = localName;
            CatchExceptionType = catchExceptionType;
        }

        public CodeCatchClause(string localName, CodeTypeReference catchExceptionType,
            params CodeStatement[] statements) : this()
        {
            LocalName = localName;
            CatchExceptionType = catchExceptionType;
            Statements.AddRange(statements);
        }

        public CodeTypeReference CatchExceptionType { get; set; }
        public string LocalName { get; set; }
        public List<CodeStatement> Statements { get; }
    }
}