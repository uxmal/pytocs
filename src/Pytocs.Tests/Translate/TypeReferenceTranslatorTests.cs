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

using Pytocs.Core.CodeModel;
using Pytocs.Core.Translate;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Pytocs.UnitTests.Translate
{
    public class TypeReferenceTranslatorTests
    {
        private string Render(CodeTypeReference ctr)
        {
            var sw = new StringWriter();
            var ind = new IndentingTextWriter(sw);
            var w = new CSharpExpressionWriter(ind);
            w.VisitTypeReference(ctr);
            return sw.ToString();
        }

        [Fact(DisplayName = nameof(Trt_VariantTuple))]
        public void Trt_VariantTuple()
        {
            var tup = new TupleType(true, DataType.Str);
            var trt = new TypeReferenceTranslator(new Dictionary<Core.Syntax.Node, DataType>());
            
            var (csType, _) = trt.Translate(tup);

            Assert.Equal("object[]", Render(csType));
        }
    }
}
