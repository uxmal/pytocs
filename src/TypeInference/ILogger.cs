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
     
        void Error(Exception ex, string format, params object [] args);
        void Error(string format, params object [] args);
        void Inform(string p);
        void Verbose(string p);
    }

    public class ConsoleLogger : ILogger{

        public void Error(Exception ex, string format, params object[] args)
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
            throw new NotImplementedException();
        }

        public TraceLevel Level { get; set; }
    }

    public class Logger : ILogger
    {
        private string title;

        public Logger(string title)
        {
            this.title = title;
        }

        public TraceLevel Level { get; set; }

        public void Error(string format, params object[] args)
        {
            Write(EventLogEntryType.Error, string.Format(format, args));
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(format, args);
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
