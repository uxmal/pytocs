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

using System.IO;

namespace Pytocs.Core.Syntax
{
    public class Argument : Node
    {
        public readonly Exp defval;
        public readonly Exp name;

        public Argument(Exp name, string filename, int start, int end)
            : base(filename, start, end)
        {
            this.name = name;
            defval = null;
        }

        public Argument(Exp name, Exp defval, string filename, int start, int end) : base(filename, start, end)
        {
            this.name = name;
            this.defval = defval;
        }

        public override string ToString()
        {
            StringWriter sw = new StringWriter();
            Write(sw);
            return sw.ToString();
        }

        public virtual void Write(TextWriter writer)
        {
            if (name != null)
            {
                name.Write(writer);
                CompFor compFor = defval as CompFor;
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
}