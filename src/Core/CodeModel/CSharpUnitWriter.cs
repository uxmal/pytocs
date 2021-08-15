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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pytocs.Core.CodeModel
{
    public class CSharpUnitWriter
    {
        private readonly IndentingTextWriter writer;

        public CSharpUnitWriter(IndentingTextWriter indentingTextWriter)
        {
            this.writer = indentingTextWriter;
        }
         
        public void Write(CodeCompileUnit unit)
        {
            foreach (var n in unit.Namespaces)
            {
                foreach (var comment in n.Comments)
                {
                    writer.Write("//");
                    writer.Write(comment.Comment);
                    writer.WriteLine();
                }
                if (!string.IsNullOrEmpty(n.Name))
                {
                    writer.Write("namespace");
                    writer.WriteName(" ");
                    writer.WriteDottedName(n.Name);
                    writer.WriteLine(" {");
                    ++writer.IndentLevel;
                }
                foreach (var imp in n.Imports)
                {
                    writer.WriteLine();
                    writer.Write("using");
                    writer.Write(" ");
                    writer.WriteDottedName(imp.Namespace);
                    writer.WriteLine(";");
                }
                foreach (var type in n.Types)
                {
                    writer.WriteLine();
                    var tw = new CSharpTypeWriter(type, writer);
                    type.Accept(tw);
                }
                if (!string.IsNullOrEmpty(n.Name))
                {
                    --writer.IndentLevel;
                    writer.WriteLine("}");
                }
            }
        }
    }
}
