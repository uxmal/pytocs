using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.TypeInference
{
    public interface ILogger
    {
        TraceLevel Level { get; set; }
     
        void Error(Exception ex, string p);
        void Inform(string p);
        void Verbose(string p);

    }

    public class Logger : ILogger
    {
        private string title;

        public Logger(string title)
        {
            this.title = title;
        }

        public TraceLevel Level { get; set; }

        public void Error(Exception ex, string msg)
        {
            var sb = new StringBuilder();
            sb.AppendLine(msg);
            while (ex != null)
            {
                sb.Append(" ");
                sb.Append(ex.Message);
                ex = ex.InnerException;
            }
            Write(EventLogEntryType.Error, sb.ToString());
        }

        public void Inform(string msg)
        {
            Write(EventLogEntryType.Information, msg);
        }

        public void Verbose(string msg)
        {
            Write(EventLogEntryType.Information, msg);
        }

        private void Write(EventLogEntryType et, string message)
        {
            Console.WriteLine("{0}: {1}: {1}",  title, et, message);
        }
    }
}
