using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.Translate
{
    public class LambdaBodyGenerator : MethodGenerator
    {
        public LambdaBodyGenerator(
            ClassDef classDef,
            FunctionDef f,
            List<Parameter> args,
            bool isStatic,
            bool isAsync,
            TypeReferenceTranslator types,
            CodeGenerator gen)
            : base(classDef, f, null, args, isStatic, isAsync, types, gen)
        {
        }

        protected override CodeMemberMethod Generate(CodeTypeReference retType,
            CodeParameterDeclarationExpression[] parms)
        {
            CodeMemberMethod method = gen.LambdaMethod(parms, () => Xlat(f.body));
            GenerateTupleParameterUnpackers(method);
            LocalVariableGenerator.Generate(method, globals);
            return method;
        }

        internal CodeVariableDeclarationStatement GenerateLambdaVariable(FunctionDef f)
        {
            CodeTypeReference type = gen.TypeRef("Func", Enumerable.Range(0, f.parameters.Count + 1)
                .Select(x => "object")
                .ToArray());
            return new CodeVariableDeclarationStatement(type, f.name.Name);
        }
    }
}