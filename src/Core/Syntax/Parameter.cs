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

using System.Collections.Generic;
using System.Text;

namespace Pytocs.Core.Syntax
{
    public class Parameter
    {
        /// <summary>
        ///     Parameter name
        /// </summary>
        public Identifier Id;

        public bool keyarg;

        /// <summary>
        ///     Default value
        /// </summary>
        public Exp test;

        public List<Parameter> tuple;

        public bool vararg;

        public string Comment { get; internal set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (tuple != null)
            {
                sb.Append("(");
                sb.Append(string.Join(",", tuple));
                sb.Append(")");
            }
            else if (keyarg)
            {
                sb.AppendFormat("**{0}", Id);
            }
            else if (vararg)
            {
                sb.AppendFormat("*{0}", Id);
            }
            else
            {
                sb.Append(Id);
                if (test != null)
                {
                    sb.AppendFormat("={0}", test);
                }
            }

            return sb.ToString();
        }
    }
}