using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.yinwang.pysonar
{
    class JsonTools
    {
    }

    public class JsonFactory
    {
        internal JsonGenerator createGenerator(System.IO.TextWriter symOut)
        {
            throw new NotImplementedException();
        }
    }

    public class JsonGenerator
    {
        internal void writeStringField(string p1, string p2)
        {
            throw new NotImplementedException();
        }

        internal void writeBooleanField(string p, bool isExported)
        {
            throw new NotImplementedException();
        }

        internal void writeNumberField(string p1, int p2)
        {
            throw new NotImplementedException();
        }

        internal void writeObjectFieldStart(string p)
        {
            throw new NotImplementedException();
        }

        internal void writeEndObject()
        {
            throw new NotImplementedException();
        }

        internal void writeStartArray()
        {
            throw new NotImplementedException();
        }

        internal void writeEndArray()
        {
            throw new NotImplementedException();
        }

        internal void close()
        {
            throw new NotImplementedException();
        }

        internal void writeStartObject()
        {
            throw new NotImplementedException();
        }

        internal void writeNullField(string p)
        {
            throw new NotImplementedException();
        }
    }
}
