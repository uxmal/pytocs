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
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core;
using Pytocs.Core.TypeInference;

namespace Pytocs.Gui
{
    public class DelegateLogger : ILogger
    {
        private readonly Func<string, Task> _action;

        public DelegateLogger(Func<string, Task> action)
        {
            _action = action;
        }

        public TraceLevel Level { get; set; } = TraceLevel.Verbose;

        public void Error(string format, params object[] args)
        {
            Write(TraceLevel.Error, string.Format(format, args));
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(format, args);
            Exception? e = ex;
            while (e != null)
            {
                sb.Append(" ");
                sb.Append(e.Message);
                e = e.InnerException;
            }
            Write(TraceLevel.Error, sb.ToString());
        }

        public void Inform(string msg)
        {
            Write(TraceLevel.Info, msg);
        }

        public void Verbose(string msg)
        {
            Write(TraceLevel.Verbose, msg);
        }

        private async void Write(TraceLevel et, string message)
        {
            if ((int) et <= (int) Level)
            {
                await _action.Invoke($"[{DateTime.Now}] [{et}] {message}\n");
            }
        }
    }
}
