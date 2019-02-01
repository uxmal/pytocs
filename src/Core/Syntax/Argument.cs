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
using System.Linq;
using System.IO;
using System.Text;

namespace Pytocs.Core.Syntax
{
    public class Argument : Node
    {
        public readonly Exp name;
        public readonly Exp defval;

        public Argument(Exp name, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.name = name;
            this.defval = null;
        }

        public Argument(Exp name, Exp defval, string filename, int start, int end) : base(filename, start, end)
        {
            this.name = name;
            this.defval = defval;
        }

        public override string ToString()
        {
            var sw = new StringWriter();
            Write(sw);
            return sw.ToString();
        }

        public virtual void Write(TextWriter writer)
        {
            if (name != null)
            {
                name.Write(writer);
                var compFor = defval as CompFor;
                if (compFor != null)
                {
                    writer.Write(" ");
                    compFor.Write(writer);
                    return;
                }
                writer.Write("=");
            }
            if (defval != null)
            {
                defval.Write(writer);
            }
        }
    }

    class ListArgument : Argument
    {
        public ListArgument(Exp t, string filename, int start, int end) : base(t, filename, start, end)
        {
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("*");
            base.Write(writer);
        }
    }

    public class KeywordArgument : Argument
    {
        public KeywordArgument(Exp t, string filename, int start, int end)
            : base(t, filename, start, end)
        {
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("**");
            base.Write(writer);
        }
    }
}
