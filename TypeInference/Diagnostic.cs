using System;

namespace Pytocs.TypeInference
{
    public class Diagnostic
    {
        public enum Category
        {
            INFO, WARNING, ERROR
        }

        public string file;
        public Category category;
        public int start;
        public int end;
        public string msg;

        public Diagnostic(string file, Category category, int start, int end, string msg)
        {
            this.category = category;
            this.file = file;
            this.start = start;
            this.end = end;
            this.msg = msg;
        }

        public override string ToString()
        {
            return "<Diagnostic:" + file + ":" + category + ":" + msg + ">";
        }
    }
}