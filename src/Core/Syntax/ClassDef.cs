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

namespace Pytocs.Core.Syntax
{
    public class ClassDef : Statement
    {
        public readonly List<Argument> args; //$REVIEW: could these be dotted names?
        public readonly SuiteStatement body;
        public readonly Identifier name;

        public ClassDef(Identifier name, List<Argument> baseClasses, SuiteStatement body, string filename, int start,
            int end) : base(filename, start, end)
        {
            this.name = name;
            args = baseClasses;
            this.body = body;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitClass(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitClass(this);
        }
    }
}