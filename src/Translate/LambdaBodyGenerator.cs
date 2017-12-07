using Pytocs.CodeModel;
using Pytocs.Syntax;
using Pytocs.TypeInference;
using Pytocs.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    public class LambdaBodyGenerator : MethodGenerator
    {
        public LambdaBodyGenerator(FunctionDef f, List<Parameter> args, bool isStatic, CodeGenerator gen)
            : base(f, null, args, isStatic, gen)
        {
        }

        protected override CodeMemberMethod Generate(CodeParameterDeclarationExpression[] parms)
        {
            var method = gen.LambdaMethod(parms, () => Xlat(f.body));
            GenerateTupleParameterUnpackers(method);
            LocalVariableGenerator.Generate(method);
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
