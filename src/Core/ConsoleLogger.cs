using System;
using System.Diagnostics;

namespace Pytocs.Core
{
    public class ConsoleLogger : ILogger
    {
        public TraceLevel Level { get; set; }

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
        }
    }
}