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

#nullable enable

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate
{
    public class LambdaBodyGenerator : MethodGenerator
    {
        public LambdaBodyGenerator(
            ClassDef? classDef, 
            FunctionDef f, 
            List<Parameter> args,
            bool isStatic,
            bool isAsync, 
            TypeReferenceTranslator types,
            CodeGenerator gen)
            : base(classDef, f, null, args, isStatic, isAsync, types, gen)
        {
        }

        protected override CodeMemberMethod Generate(CodeTypeReference retType, CodeParameterDeclarationExpression[] parms)
        {
            var method = gen.LambdaMethod(parms, () => Xlat(f.body));
            GenerateTupleParameterUnpackers(method);
            LocalVariableGenerator.Generate(method, globals);
            return method;
        }

        internal CodeVariableDeclarationStatement GenerateLambdaVariable(FunctionDef f)
        {
            var type = this.gen.TypeRef("Func", Enumerable.Range(0, f.parameters.Count + 1)
                .Select(x => "object")
                .ToArray());
            return new CodeVariableDeclarationStatement(type, f.name.Name);
        }
    }
}
