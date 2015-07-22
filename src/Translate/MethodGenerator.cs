using Pytocs.CodeModel;
using Pytocs.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    public class MethodGenerator
    {
        protected FunctionDef f;
        protected string fnName;
        protected List<Parameter> args;
        private bool isStatic;
        protected ExpTranslator xlat;
        protected StatementTranslator stmtXlat;
        protected CodeGenerator gen;
        private HashSet<string> locals;

        public MethodGenerator(FunctionDef f, string fnName, List<Parameter> args, bool isStatic, CodeGenerator gen)
        {
            this.f = f;
            this.fnName = fnName;
            this.args = args;
            this.isStatic = isStatic;
            this.gen = gen;
        }
        
        public CodeMemberMethod Generate()
        {
            this.locals = new HashSet<string>();
            this.xlat = new ExpTranslator(gen);
            this.stmtXlat = new StatementTranslator(gen, locals);

            int iFirstDefaultValue = -1;
            for (var i = 0; i < args.Count; ++i)
            {
                if (args[i].test != null || iFirstDefaultValue != -1)
                {
                    if (iFirstDefaultValue == -1)
                        iFirstDefaultValue = i;
                    GenerateDefaultArgMethod(i);
                }
            }
            return Generate(CreateFunctionParameters(args)); // () => bodyGenerator(f.body));
        }

        protected virtual CodeMemberMethod Generate(CodeParameterDeclarationExpression[] parms)
        {
            CodeMemberMethod method;
            if (isStatic)
            {
                method = gen.StaticMethod(fnName, parms, () => Xlat(f.body));
            }
            else
            {
                method = gen.Method(fnName, parms, () => Xlat(f.body));
            }
            GenerateLocalVariables(method);
            return method;
        }

        protected void GenerateLocalVariables(CodeMemberMethod method)
        {
            method.Statements.InsertRange(
                0,
                locals
                    .OrderBy(l => l)
                    .Select(l => new CodeVariableDeclarationStatement("object", l)));
        }

        private void Xlat(SuiteStatement suite)
        {
            var comments = StatementTranslator.ConvertFirstStringToComments(suite.stmts);
            stmtXlat.Xlat(suite);
            gen.CurrentMethod.Comments.AddRange(comments);
        }

        private CodeParameterDeclarationExpression[] CreateFunctionParameters(IEnumerable<Parameter> args)
        {
            return args
                .OrderBy(ta => ta.vararg)
                .Select(ta => GenerateFunctionParameter(ta)).ToArray();
        }

        private CodeParameterDeclarationExpression GenerateFunctionParameter(Parameter ta)
        {
            var parameterType = new CodeTypeReference(typeof(object));
            if (ta.keyarg)
            {
                parameterType = new CodeTypeReference("Hashtable");
                gen.EnsureImport("System.Collections");
            }

            return new CodeParameterDeclarationExpression
            {
                ParameterType =  parameterType,
                ParameterName = ta.Id.Name,
                IsVarargs = ta.vararg
            };
        }

        private void GenerateDefaultArgMethod(int iFirstDefault)
        {
            var argList = args.Take(iFirstDefault).Select(p => new CodeParameterDeclarationExpression
            {
                ParameterType = new CodeTypeReference(typeof(object)),
                ParameterName = p.Id.Name,
                IsVarargs = p.vararg,
            });
            var paramList = new List<CodeExpression>();
            for (int i = 0; i < args.Count; ++i)
            {
                paramList.Add((i < iFirstDefault || args[i].test == null)
                    ? new CodeVariableReferenceExpression(args[i].Id.Name)
                    : args[i].test.Accept(xlat));
            }

            GenerateDefaultArgMethod(argList.ToArray(), paramList.ToArray());
        }

        protected virtual void GenerateDefaultArgMethod(
            CodeParameterDeclarationExpression[] argList,
            CodeExpression [] paramList)
        {
            if (isStatic) 
            {
                gen.StaticMethod(fnName, argList, () =>
                {
                    gen.Return(gen.Appl(
                        new CodeVariableReferenceExpression(fnName),
                        paramList));
                });
            }
            else 
            {
                gen.Method(fnName, argList, () =>
                {
                    gen.Return(gen.Appl(
                        new CodeVariableReferenceExpression(fnName),
                        paramList));
                });
            }
        }
    }
}
