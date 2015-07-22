using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Syntax
{
    public class Parameter
    {
        /// <summary>
        /// Parameter name
        /// </summary>
        public Identifier Id;

        /// <summary>
        /// Default value
        /// </summary>
        public Exp test;
        public bool vararg;
        public bool keyarg;

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (keyarg)
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
