#region License
//  Copyright 2015 John Källén
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
