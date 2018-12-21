using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Pytocs.TypeInference;

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
            while (ex != null)
            {
                sb.Append(" ");
                sb.Append(ex.Message);
                ex = ex.InnerException;
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
