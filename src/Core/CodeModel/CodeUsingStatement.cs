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

using System;
using System.Collections.Generic;

namespace Pytocs.Core.CodeModel
{
    public class CodeUsingStatement : CodeStatement
    {
        public CodeUsingStatement()
        {
            Initializers = new List<CodeStatement>();
            Statements = new List<CodeStatement>();
        }

        [Obsolete]
        public CodeUsingStatement(CodeExpression name, CodeExpression value) : this()
        {
            Initializers.Add(new CodeAssignStatement(name, value));
        }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitUsing(this);
        }

        public List<CodeStatement> Initializers { get; private set; }
        public List<CodeStatement> Statements { get; private set; }
    }
}