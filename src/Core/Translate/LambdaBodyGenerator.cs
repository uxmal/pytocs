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
        public LambdaBodyGenerator(FunctionDef f, List<Parameter> args,
            bool isStatic,
            bool isAsync, 
            TypeReferenceTranslator types,
            CodeGenerator gen)
            : base(f, null, args, isStatic, isAsync, types, gen)
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
