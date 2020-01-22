using System;
using System.Diagnostics;
using System.Text;

namespace Pytocs.Core
{
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

        public void Error(Exception ex, string format, params object[] args)
        {
            StringBuilder sb = new StringBuilder();
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