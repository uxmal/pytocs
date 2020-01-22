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

namespace Pytocs.Core.Syntax
{
    public class VarArg
    {
        public Exp name;    // could be tuple
        public Exp test;
        public bool IsKeyword;
        public bool IsIndexed;

        public static VarArg Keyword(Identifier name)
        {
            return new VarArg { name = name, IsKeyword = true };
        }

        public static VarArg Indexed(Identifier name)
        {
            return new VarArg { name = name, IsIndexed = true };
        }
    }
}