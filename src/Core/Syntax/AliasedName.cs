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
    public class AliasedName : Node
    {
        public readonly Identifier alias;
        public readonly DottedName orig;

        public AliasedName(Identifier orig, Identifier alias, string filename, int start, int end) : base(filename,
            start, end)
        {
            this.orig = new DottedName(new List<Identifier> { orig }, filename, start, end);
            this.alias = alias;
        }

        public AliasedName(DottedName orig, Identifier alias, string filename, int start, int end) : base(filename,
            start, end)
        {
            this.orig = orig;
            this.alias = alias;
        }
    }
}