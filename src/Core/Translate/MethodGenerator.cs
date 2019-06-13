    #region License
//  Copyright 2015-2018 John Källén
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
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Translate
{
    /// <summary>
    /// Generates code for a Python function definition.
    /// </summary>
    public class MethodGenerator
    {
        protected FunctionDef f;
        protected string fnName;
        protected List<Parameter> args;
        private bool isStatic;
        private bool isAsync;
        protected ExpTranslator xlat;
        protected StatementTranslator stmtXlat;
        protected CodeGenerator gen;
        private Dictionary<Parameter, CodeParameterDeclarationExpression> mpPyParamToCs;
        private SymbolGenerator gensym;
        private TypeReferenceTranslator types;
        protected HashSet<string> globals;

        public MethodGenerator(
            FunctionDef f, 
            string fnName,
            List<Parameter> args,
            bool isStatic, 
            bool isAsync,
            TypeReferenceTranslator types,
            CodeGenerator gen)
        {
            this.f = f;
            this.fnName = fnName;
            this.args = args;
            this.isStatic = isStatic;
            this.isAsync = isAsync;
            this.gen = gen;
            this.gensym = new SymbolGenerator();
            this.types = new TypeReferenceTranslator(new Dictionary<Node, DataType>());
            this.xlat = new ExpTranslator(this.types, gen, gensym);
            this.globals = new HashSet<string>();
            this.stmtXlat = new StatementTranslator(types, gen, gensym, globals);
        }

        public CodeMemberMethod Generate()
        {
            return Generate(CreateReturnType(), CreateFunctionParameters(args)); // () => bodyGenerator(f.body));
        }

        protected virtual CodeMemberMethod Generate(CodeTypeReference retType, CodeParameterDeclarationExpression[] parms)
        {
            CodeMemberMethod method;
            if (isStatic)
            {
                method = gen.StaticMethod(fnName, retType, parms, () => Xlat(f.body));
            }
            else
            {
                method = gen.Method(fnName, retType, parms, () => Xlat(f.body));
            }
            method.IsAsync = isAsync;
            GenerateTupleParameterUnpackers(method);
            LocalVariableGenerator.Generate(method, globals);
            return method;
        }

        protected void GenerateTupleParameterUnpackers(CodeMemberMethod method)
        {
            foreach (var parameter in args.Where(p => p.tuple != null))
            {
                var csTupleParam = mpPyParamToCs[parameter];
                var tuplePath = new CodeVariableReferenceExpression(csTupleParam.ParameterName);
                foreach (var component in parameter.tuple.Select((p, i) => new { p, i = i + 1 }))
                {
                    GenerateTupleParameterUnpacker(component.p, component.i, tuplePath, method);
                }
            }
        }

        private void GenerateTupleParameterUnpacker(Parameter p, int i, CodeExpression tuplePath, CodeMemberMethod method)
        {
            if (p.Id.Name == "_")
                return;
            method.Statements.Insert(0, new CodeVariableDeclarationStatement("object", p.Id.Name)
            {
                InitExpression = gen.Access(tuplePath, "Item" + i)
            });
        }

        public void Xlat(SuiteStatement suite)
        {
            var comments = StatementTranslator.ConvertFirstStringToComments(suite.stmts);
            stmtXlat.Xlat(suite);
            gen.CurrentMemberComments.AddRange(comments);
        }

        private CodeTypeReference CreateReturnType()
        {
            Type tyRet;
            if (isAsync)
                tyRet = typeof(Task<object>);
            else
                tyRet = typeof(object);
            return new CodeTypeReference(tyRet);
        }

        private CodeParameterDeclarationExpression[] CreateFunctionParameters(IEnumerable<Parameter> parameters)
        {
            var convs = parameters
                .OrderBy(ta => ta.vararg)
                .Where(ta => !ta.vararg || ta.Id != null)
                .Select(ta => (ta, GenerateFunctionParameter(ta))).ToArray();
            this.mpPyParamToCs = convs.ToDictionary(k => k.Item1, v => v.Item2);
            return convs.Select(c => c.Item2).ToArray();
        }

        private CodeParameterDeclarationExpression GenerateFunctionParameter(Parameter ta)
        {
            CodeTypeReference parameterType;
            if (ta.tuple != null)
            {
                parameterType = GenerateTupleParameterType(ta.tuple);
                return new CodeParameterDeclarationExpression
                {
                    ParameterType = parameterType,
                    ParameterName = stmtXlat.GenSymParameter("_tup_", parameterType).Name,
                    IsVarargs = false,
                };
            }
            else if (ta.keyarg)
            {
                parameterType = new CodeTypeReference("Hashtable");
                gen.EnsureImport("System.Collections");
            }
            else
            {
                parameterType = new CodeTypeReference(typeof(object));
            }
            return new CodeParameterDeclarationExpression
            {
                ParameterType = parameterType,
                ParameterName = ta.Id?.Name,
                IsVarargs = ta.vararg,
                DefaultValue = ta.test?.Accept(this.xlat)
            };
        }

        private CodeTypeReference GenerateTupleParameterType(List<Parameter> list)
        {
            var types = list.Select(p => p.tuple != null
                ? GenerateTupleParameterType(p.tuple)
                : new CodeTypeReference(typeof(object)));
            return new CodeTypeReference("Tuple", types.ToArray());
        }
    }
}
