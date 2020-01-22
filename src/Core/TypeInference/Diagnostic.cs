#region License

//  Copyright 2015-2020 John K�ll�n
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

#endregion License

namespace Pytocs.Core.TypeInference
{
    public class Diagnostic
    {
        public enum Category
        {
            INFO,
            WARNING,
            ERROR
        }

        public Category category;
        public int end;

        public string file;
        public string msg;
        public int start;

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