#region License
//  Copyright 2015-2021 John Källén
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

namespace Pytocs.Core.TypeInference
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