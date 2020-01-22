using System;
using System.Diagnostics;

namespace Pytocs.Core
{
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
}