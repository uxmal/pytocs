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
using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate
{
    public class FunctionGenerator : MethodGenerator
    {
        public FunctionGenerator(
            FunctionDef f,
            string fnName,
            List<Parameter> args,
            bool isStatic,
            bool isAsync,
            TypeReferenceTranslator types,
            CodeGenerator gen) : base(
                null, f, fnName, args, isStatic, isAsync, types, gen)
        {
        }

        protected override ICodeFunction Generate(CodeTypeReference retType, CodeParameterDeclarationExpression[] parms)
        {
            var func = gen.LocalFunction(fnName!, retType, parms, () => Xlat(f.body));
            func.IsAsync = isAsync;
            return func;
        }
    }
}
