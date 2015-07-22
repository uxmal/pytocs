using Pytocs.CodeModel;
using Pytocs.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    public class ConstructorGenerator : MethodGenerator
    {
        public ConstructorGenerator(FunctionDef f, List<Syntax.Parameter> args, CodeGenerator gen)
            : base(f, "", args, false, gen)
        {
        }

        protected override CodeMemberMethod Generate(CodeParameterDeclarationExpression[] parms)
        {
            var cons = gen.Constructor(parms, () => XlatConstructor(f.body));
            GenerateLocalVariables(cons);
            return cons;
        }

        private void XlatConstructor(SuiteStatement stmt)
        {
            if (stmt == null)
                return;

            var comments = StatementTranslator.ConvertFirstStringToComments(stmt.stmts);
            stmt.Accept(this.stmtXlat);
            if (gen.Scope.Count == 0)
                return;
            gen.Scope[0].ToString();
            var expStm = gen.Scope[0] as CodeExpressionStatement;
            if (expStm == null)
                return;
            var appl = expStm.Expression as CodeApplicationExpression;
            if (appl == null)
                return;
            var method = appl.Method as CodeFieldReferenceExpression;
            if (method == null || method.FieldName != "__init__")
                return;
            var ctor = (CodeConstructor) gen.CurrentMethod;
            ctor.Comments.AddRange(comments);
            ctor.BaseConstructorArgs.AddRange(appl.Arguments.Skip(1));
            gen.Scope.RemoveAt(0);
        }

        protected override void GenerateDefaultArgMethod(
            CodeParameterDeclarationExpression[] argList,
            CodeExpression [] paramList)
        {
            var cons = gen.Constructor(argList, () => {});
            cons.ChainedConstructorArgs.AddRange(paramList);
        }
    }
}
