using System;
using System.Diagnostics;

namespace Pytocs.UnitTests
{
    public class FakeLogger : ILogger
    {
        public TraceLevel Level { get; set; }

        public void Error(string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void Inform(string p)
        {
            throw new NotImplementedException();
        }

        public void Verbose(string p)
        {
            Debug.WriteLine(p);
        }
    }
}