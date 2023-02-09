#region License
//  Copyright 2015-2021 John Källén
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core
{
    public interface ILogger
    {
        TraceLevel Level { get; set; }
     
        void Error(Exception ex, string format, params object [] args);
        void Error(string format, params object [] args);
        void Inform(string p);
        void Verbose(string p);
    }

    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        public TraceLevel Level { get; set; }

        public void Error(Exception ex, string format, params object[] args)
        {
        }

        public void Error(string format, params object[] args)
        {
        }

        public void Inform(string p)
        {
        }

        public void Verbose(string p)
        {
        }
    }

    public class ConsoleLogger : ILogger
    {
        public void Error(Exception? ex, string format, params object[] args)
        {
            Console.Error.Write("Error: ");
            Console.Error.WriteLine(format, args);
            while (ex != null)
            {
                Console.Error.WriteLine(" {0}", ex.Message);
                ex = ex.InnerException;
            }
        }

        public void Error(string format, params object[] args)
        {
            Console.Error.Write("Error: ");
            Console.Error.WriteLine(format, args);
        }

        public void Inform(string p)
        {
            throw new NotImplementedException();
        }

        public void Verbose(string p)
        {
        }

        public TraceLevel Level { get; set; }
    }

    public class Logger : ILogger
    {
        private readonly string title;

        public Logger(string title)
        {
            this.title = title;
        }

        public TraceLevel Level { get; set; }

        public void Error(string format, params object[] args)
        {
            Write(TraceEventType.Error, string.Format(format, args));
        }

        public void Error(Exception? ex, string format, params object[] args)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(format, args);
            while (ex != null)
            {
                sb.Append(" ");
                sb.Append(ex.Message);
                ex = ex.InnerException;
            }
            Write(TraceEventType.Error, sb.ToString());
        }

        public void Inform(string msg)
        {
            Write(TraceEventType.Information, msg);
        }

        public void Verbose(string msg)
        {
            Write(TraceEventType.Information, msg);
        }

        private void Write(TraceEventType et, string message)
        {
            Console.WriteLine("{0}: {1}: {2}", title, et, message);
        }
    }
}
