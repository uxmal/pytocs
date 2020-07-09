#region License
//  Copyright 2015-2020 John Källén
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

#nullable enable

namespace Pytocs.Core.Translate
{
    /// <summary>
    /// Generates code for a Python function definition.
    /// </summary>
    public class MethodGenerator
    {
        private readonly ClassDef? classDef;
        protected readonly FunctionDef f;
        protected readonly string? fnName;
        protected readonly List<Parameter> args;
        private readonly bool isStatic;
        private readonly bool isAsync;
        protected ExpTranslator xlat;
        protected StatementTranslator stmtXlat;
        protected CodeGenerator gen;
        private Dictionary<Parameter, CodeParameterDeclarationExpression>? mpPyParamToCs;
        private SymbolGenerator gensym;
        private TypeReferenceTranslator types;
        protected HashSet<string> globals;

        public MethodGenerator(
            ClassDef? classDef,
            FunctionDef f, 
            string? fnName,
            List<Parameter> args,
            bool isStatic, 
            bool isAsync,
            TypeReferenceTranslator types,
            CodeGenerator gen)
        {
            this.classDef = classDef;
            this.f = f;
            this.fnName = fnName;
            this.args = args;
            this.isStatic = isStatic;
            this.isAsync = isAsync;
            this.gen = gen;
            this.gensym = new SymbolGenerator();
            this.types = types;
            this.xlat = new ExpTranslator(classDef, this.types, gen, gensym);
            this.globals = new HashSet<string>();
            this.stmtXlat = new StatementTranslator(classDef, types, gen, gensym, globals);
        }

        /// <summary>
        /// Generate a C# method implementation from the Python function definition.
        /// </summary>
        public CodeMemberMethod Generate()
        {
            var parameters = CreateFunctionParameters(args);
            var returnType = CreateReturnType();
            return Generate(returnType, parameters);
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
                var csTupleParam = mpPyParamToCs![parameter];
                var tuplePath = new CodeVariableReferenceExpression(csTupleParam.ParameterName);
                foreach (var component in parameter.tuple.Select((p, i) => new { p, i = i + 1 }))
                {
                    GenerateTupleParameterUnpacker(component.p, component.i, tuplePath, method);
                }
            }
        }

        private void GenerateTupleParameterUnpacker(Parameter p, int i, CodeExpression tuplePath, CodeMemberMethod method)
        {
            if (p.Id?.Name == "_")
                return;
            method.Statements.Insert(0, new CodeVariableDeclarationStatement("object", p.Id!.Name)
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
            var pyType = this.types.TypeOf(f.name);
            CodeTypeReference dtRet;
            if (pyType is FunType fnType)
            {
                pyType = fnType.GetReturnType();
                (dtRet, _) = this.types.Translate(pyType);
            }
            else
            {
                dtRet = new CodeTypeReference(typeof(object));
            }
            if (isAsync)
            {
                gen.EnsureImport(TypeReferenceTranslator.TasksNamespace);
                dtRet = gen.TypeRef("Task", dtRet);
            }
            return dtRet;
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
                var (dtParam, _)= types.TranslateTypeOf(ta.Id!);
                parameterType = dtParam;
            }
            return new CodeParameterDeclarationExpression
            {
                ParameterType = parameterType,
                ParameterName = ta.Id?.Name,
                IsVarargs = ta.vararg,
                DefaultValue = ta.Test?.Accept(this.xlat)
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
