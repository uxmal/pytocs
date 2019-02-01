#region License
//  Copyright 2015-2018 John Källén
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
using System.IO;
using System.Text;

namespace Pytocs.Core.Syntax
{
    public class AliasedExp : Exp
    {
        public Exp exp;
        public Identifier alias;

        public AliasedExp(Exp t, Identifier alias, string filename, int start, int end) : base(filename, start, end)
        {
            this.exp = t;
            this.alias = alias;
        }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitAliasedExp(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitAliasedExp(this);
        }

        public override void Write(System.IO.TextWriter writer)
        {
            if (exp == null)
                return;
            writer.Write(alias != null ? " {0}, {1}" : "{0}", exp, alias);
        }
    }
}
