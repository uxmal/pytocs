﻿#region License

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

#endregion License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.CodeModel
{
    public class CodeGenerator
    {
        private bool isInit;
        private readonly CSharpCodeProvider provider;
        private readonly CodeCompileUnit unt;

        public CodeGenerator(CodeCompileUnit unt, string modulePath, string moduleName)
        {
            this.unt = unt;
            isInit = moduleName == "__init__";
            provider = new CSharpCodeProvider();
            Scope = new List<CodeStatement>(); // dummy scope.
            CurrentNamespace = new CodeNamespace(modulePath);
            CurrentType = new CodeTypeDeclaration(moduleName)
            {
                IsClass = true,
                Attributes = MemberAttributes.Static | MemberAttributes.Public
            };
            CurrentNamespace.Types.Add(CurrentType);
            unt.Namespaces.Add(CurrentNamespace);
        }

        public List<CodeStatement> Scope { get; private set; }
        public CodeMember CurrentMember { get; private set; }
        public List<CodeStatement> CurrentMemberStatements { get; private set; }
        public List<CodeCommentStatement> CurrentMemberComments { get; private set; }
        public CodeNamespace CurrentNamespace { get; set; }
        public CodeTypeDeclaration CurrentType { get; set; }

        public CodeExpression Access(CodeExpression exp, string fieldName)
        {
            return new CodeFieldReferenceExpression(exp, fieldName);
        }

        public CodeBinaryOperatorExpression BinOp(CodeExpression l, CodeOperatorType op, CodeExpression r)
        {
            return new CodeBinaryOperatorExpression(l, op, r);
        }

        public CodeCatchClause CatchClause(string localName, CodeTypeReference type, Action generateClauseBody)
        {
            CodeCatchClause clause = new CodeCatchClause(localName, type);
            List<CodeStatement> oldScope = Scope;
            Scope = clause.Statements;
            generateClauseBody();
            Scope = oldScope;
            return clause;
        }

        public CodeTypeDeclaration Class(
            string name,
            IEnumerable<string> baseClasses,
            Func<IEnumerable<CodeMemberField>> fieldGenerator,
            Action bodyGenerator)
        {
            List<CodeStatement> oldScope = Scope;
            Scope = new List<CodeStatement>();

            CodeTypeDeclaration c = new CodeTypeDeclaration
            {
                IsClass = true,
                Name = name
            };

            // classes in __init__ files go directly into the namespace.
            if (isInit)
            {
                CurrentNamespace.Types.Add(c);
            }
            else
            {
                AddMemberWithComments(c);
            }

            c.BaseTypes.AddRange(baseClasses.Select(b => new CodeTypeReference(b)).ToArray());
            CodeTypeDeclaration old = CurrentType;
            CodeMember oldMethod = CurrentMember;
            List<CodeStatement> oldStmts = CurrentMemberStatements;
            List<CodeCommentStatement> oldComments = CurrentMemberComments;
            bool oldIsInit = isInit;
            CurrentType = c;
            CurrentMember = null;
            CurrentMemberStatements = null;
            CurrentMemberComments = null;
            isInit = false;
            c.Members.AddRange(fieldGenerator());
            bodyGenerator();
            CurrentMember = oldMethod;
            CurrentMemberStatements = oldStmts;
            CurrentMemberComments = oldComments;

            CurrentType = old;
            isInit = oldIsInit;
            Scope = oldScope;
            return c;
        }

        private void AddMemberWithComments(CodeMember c)
        {
            c.Comments.AddRange(Scope.OfType<CodeCommentStatement>());
            Scope.RemoveAll(s => s is CodeCommentStatement);
            CurrentType.Members.Add(c);
        }

        public CodeAssignStatement Assign(CodeExpression lhs, CodeExpression rhs)
        {
            CodeAssignStatement ass = new CodeAssignStatement(lhs, rhs);
            Scope.Add(ass);
            return ass;
        }

        public CodeConditionStatement If(CodeExpression test, Action xlatThen, Action xlatElse)
        {
            CodeConditionStatement i = new CodeConditionStatement
            {
                Condition = test
            };
            Scope.Add(i);
            List<CodeStatement> old = Scope;
            Scope = i.TrueStatements;
            xlatThen();
            Scope = i.FalseStatements;
            xlatElse();
            Scope = old;
            return i;
        }

        public CodeStatement Foreach(CodeExpression exp, CodeExpression list, Action xlatLoopBody)
        {
            CodeForeachStatement c = new CodeForeachStatement(exp, list);
            Scope.Add(c);
            List<CodeStatement> old = Scope;
            Scope = c.Statements;
            xlatLoopBody();
            Scope = old;
            return c;
        }

        public CodeExpression Appl(CodeExpression fn, params CodeExpression[] args)
        {
            return new CodeApplicationExpression(fn, args);
        }

        public CodeExpression Sub(CodeExpression minuend, CodeExpression subtrahend)
        {
            return new CodeBinaryOperatorExpression(minuend, CodeOperatorType.Sub, subtrahend);
        }

        public CodeExpression Add(CodeExpression augend, CodeExpression addend)
        {
            return new CodeBinaryOperatorExpression(augend, CodeOperatorType.Add, addend);
        }

        public CodeExpression Div(CodeExpression dividend, CodeExpression divisor)
        {
            return new CodeBinaryOperatorExpression(dividend, CodeOperatorType.Div, divisor);
        }

        public CodeExpression Mul(CodeExpression multiplicand, CodeExpression multiplier)
        {
            return new CodeBinaryOperatorExpression(multiplicand, CodeOperatorType.Mul, multiplier);
        }

        public CodeExpression ApplyMethod(CodeExpression obj, string method, params CodeExpression[] args)
        {
            return new CodeApplicationExpression(
                MethodRef(obj, method),
                args);
        }

        public void SetCurrentMethod(CodeMemberMethod method)
        {
            CurrentMember = method;
            CurrentMemberStatements = method.Statements;
            CurrentMemberComments = method.Comments;
        }

        public void SetCurrentPropertyAccessor(
            CodeMemberProperty property,
            List<CodeStatement> stmts)
        {
            CurrentMember = property;
            CurrentMemberStatements = stmts;
            CurrentMemberComments = property.Comments;
        }

        public CodeStatement SideEffect(CodeExpression exp)
        {
            CodeExpressionStatement sideeffect = new CodeExpressionStatement(exp);
            Scope.Add(sideeffect);
            return sideeffect;
        }

        public CodeConstructor Constructor(IEnumerable<CodeParameterDeclarationExpression> parms, Action body)
        {
            CodeConstructor cons = new CodeConstructor
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            cons.Parameters.AddRange(parms.ToArray());
            AddMemberWithComments(cons);

            GenerateMethodBody(cons, body);
            return cons;
        }

        public CodeMemberMethod Method(string name, CodeTypeReference retValue,
            IEnumerable<CodeParameterDeclarationExpression> parms, Action body)
        {
            CodeMemberMethod method = new CodeMemberMethod
            {
                Name = name,
                Attributes = MemberAttributes.Public,
                ReturnType = retValue
            };
            method.Parameters.AddRange(parms.ToArray());
            AddMemberWithComments(method);

            GenerateMethodBody(method, body);
            return method;
        }

        public CodeMemberMethod StaticMethod(string name, CodeTypeReference retType,
            IEnumerable<CodeParameterDeclarationExpression> parms, Action body)
        {
            CodeMemberMethod method = new CodeMemberMethod
            {
                Name = name,
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                ReturnType = retType
            };
            method.Parameters.AddRange(parms.ToArray());
            AddMemberWithComments(method);

            GenerateMethodBody(method, body);
            return method;
        }

        public CodeMemberMethod LambdaMethod(IEnumerable<CodeParameterDeclarationExpression> parms, Action body)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Parameters.AddRange(parms.ToArray());
            GenerateMethodBody(method, body);
            return method;
        }

        private void GenerateMethodBody(CodeMemberMethod method, Action body)
        {
            List<CodeStatement> old = Scope;
            CodeMember oldMethod = CurrentMember;
            List<CodeStatement> oldStatements = CurrentMemberStatements;
            List<CodeCommentStatement> oldComments = CurrentMemberComments;
            SetCurrentMethod(method);
            Scope = method.Statements;
            body();
            Scope = old;
            CurrentMember = oldMethod;
            CurrentMemberStatements = oldStatements;
            CurrentMemberComments = oldComments;
        }

        public CodeParameterDeclarationExpression Param(Type type, string name)
        {
            return new CodeParameterDeclarationExpression(type, name);
        }

        public CodeParameterDeclarationExpression Param(Type type, string name, CodeExpression defaultValue)
        {
            return new CodeParameterDeclarationExpression(type, name, defaultValue);
        }

        public void Return(CodeExpression e = null)
        {
            Scope.Add(new CodeMethodReturnStatement(e));
        }

        public void Using(string @namespace)
        {
            CurrentNamespace.Imports.Add(new CodeNamespaceImport(@namespace));
        }

        public void Using(string alias, string @namespace)
        {
            CurrentNamespace.Imports.Add(new CodeNamespaceImport(
                EscapeKeywordName(alias) +
                " = " +
                EscapeKeywordName(@namespace)));
        }

        public string EscapeKeywordName(string name)
        {
            return IndentingTextWriter.NameNeedsQuoting(name)
                ? "@" + name
                : name;
        }

        public CodeMemberField Field(
            CodeTypeReference fieldType,
            string fieldName)
        {
            CodeMemberField field = new CodeMemberField(fieldType, fieldName)
            {
                Attributes = MemberAttributes.Public
            };
            AddMemberWithComments(field);
            return field;
        }

        public CodeMemberField Field(string fieldName, CodeExpression initializer)
        {
            CodeMemberField field = new CodeMemberField(typeof(object), fieldName)
            {
                Attributes = MemberAttributes.Public,
                InitExpression = initializer
            };
            AddMemberWithComments(field);
            return field;
        }

        public CodeArrayIndexerExpression Aref(CodeExpression exp, CodeExpression[] indices)
        {
            return new CodeArrayIndexerExpression(exp, indices);
        }

        public CodeAwaitExpression Await(CodeExpression exp)
        {
            return new CodeAwaitExpression(exp);
        }

        public CodeThrowExceptionStatement Throw(CodeExpression codeExpression)
        {
            CodeThrowExceptionStatement t = new CodeThrowExceptionStatement(codeExpression);
            Scope.Add(t);
            return t;
        }

        public CodeThrowExceptionStatement Throw()
        {
            CodeThrowExceptionStatement t = new CodeThrowExceptionStatement();
            Scope.Add(t);
            return t;
        }

        public CodeExpression Lambda(CodeExpression[] args, CodeExpression expr)
        {
            return new CodeLambdaExpression(args, expr);
        }

        public CodeExpression Lambda(CodeExpression[] args, List<CodeStatement> stmts)
        {
            return new CodeLambdaExpression(args, stmts);
        }

        public CodeExpression ListInitializer(CodeTypeReference elemType, IEnumerable<CodeExpression> exprs)
        {
            EnsureImport("System.Collections.Generic");
            CodeObjectCreateExpression list = new CodeObjectCreateExpression
            {
                Type = new CodeTypeReference("List", elemType)
            };
            list.Initializers.AddRange(exprs);
            return list;
        }

        public CodeExpression Base()
        {
            return new CodeBaseReferenceExpression();
        }

        public void EnsureImport(string nmespace)
        {
            if (CurrentNamespace.Imports.Where(i => i.Namespace == nmespace).Any())
            {
                return;
            }

            CurrentNamespace.Imports.Add(new CodeNamespaceImport(nmespace));
        }

        public CodeCastExpression Cast(CodeTypeReference type, CodeExpression exp)
        {
            return new CodeCastExpression(type, exp);
        }

        public void EnsureImports(IEnumerable<string> nmespaces)
        {
            if (nmespaces == null)
            {
                return;
            }

            foreach (string nmspace in nmespaces)
            {
                EnsureImport(nmspace);
            }
        }

        public CodeTryCatchFinallyStatement Try(
            Action genTryStatements,
            IEnumerable<CodeCatchClause> catchClauses,
            Action genFinallyStatements)
        {
            CodeTryCatchFinallyStatement t = new CodeTryCatchFinallyStatement();
            List<CodeStatement> oldScope = Scope;
            Scope = t.TryStatements;
            genTryStatements();
            t.CatchClauses.AddRange(catchClauses);
            Scope = t.FinallyStatements;
            genFinallyStatements();
            Scope = oldScope;
            Scope.Add(t);
            return t;
        }

        public void Break()
        {
            Scope.Add(new CodeBreakStatement());
        }

        public void Continue()
        {
            Scope.Add(new CodeContinueStatement());
        }

        public CodePreTestLoopStatement While(
            CodeExpression exp,
            Action generateBody)
        {
            CodePreTestLoopStatement w = new CodePreTestLoopStatement
            {
                Test = exp
            };
            List<CodeStatement> oldScope = Scope;
            Scope = w.Body;
            generateBody();
            Scope = oldScope;
            Scope.Add(w);
            return w;
        }

        public CodePostTestLoopStatement DoWhile(
            Action generateBody,
            CodeExpression exp)
        {
            CodePostTestLoopStatement dw = new CodePostTestLoopStatement
            {
                Test = exp
            };
            Scope.Add(dw);
            List<CodeStatement> oldScope = Scope;
            Scope = dw.Body;
            generateBody();
            Scope = oldScope;
            return dw;
        }

        public CodeYieldStatement Yield(CodeExpression exp)
        {
            CodeYieldStatement y = new CodeYieldStatement(exp);
            Scope.Add(y);
            return y;
        }

        public CodeMethodReferenceExpression MethodRef(CodeExpression exp, string methodName)
        {
            return new CodeMethodReferenceExpression(exp, methodName);
        }

        public CodeObjectCreateExpression New(CodeTypeReference type, params CodeExpression[] args)
        {
            CodeObjectCreateExpression exp = new CodeObjectCreateExpression
            {
                Type = type
            };
            exp.Arguments.AddRange(args);
            return exp;
        }

        public CodeArrayCreateExpression NewArray(CodeTypeReference type, params CodeExpression[] items)
        {
            CodeArrayCreateExpression exp = new CodeArrayCreateExpression(type, items);
            return exp;
        }

        public CodeNumericLiteral Number(string sNumber)
        {
            return new CodeNumericLiteral(sNumber);
        }

        public CodePrimitiveExpression Prim(object o)
        {
            if (o is long l)
            {
                if (int.MinValue <= l && l < int.MaxValue)
                {
                    o = (int)l;
                }
            }

            return new CodePrimitiveExpression(o);
        }

        public CodeTypeReference TypeRef(Type type)
        {
            return new CodeTypeReference(type);
        }

        public CodeTypeReference TypeRef(string typeName)
        {
            return new CodeTypeReference(typeName);
        }

        public CodeTypeReference TypeRef(string typeName, params string[] genericArgs)
        {
            return new CodeTypeReference(
                typeName,
                genericArgs.Select(ga => new CodeTypeReference(ga)).ToArray());
        }

        public CodeTypeReference TypeRef(string typeName, params CodeTypeReference[] genericArgs)
        {
            return new CodeTypeReference(typeName, genericArgs);
        }

        public CodeAttributeDeclaration CustomAttr(CodeTypeReference typeRef, params CodeAttributeArgument[] args)
        {
            return new CodeAttributeDeclaration
            {
                AttributeType = typeRef,
                Arguments = args.ToList()
            };
            throw new NotImplementedException();
        }

        public CodeCommentStatement Comment(string comment)
        {
            CodeCommentStatement c = new CodeCommentStatement(comment);
            Scope.Add(c);
            return c;
        }

        public CodeExpression TypeRefExpr(string typeName)
        {
            return new CodeTypeReferenceExpression(typeName);
        }

        public CodeUsingStatement Using(
            IEnumerable<CodeStatement> initializers,
            Action xlatUsingBody)
        {
            CodeUsingStatement u = new CodeUsingStatement();
            Scope.Add(u);
            u.Initializers.AddRange(initializers);
            List<CodeStatement> old = Scope;
            Scope = u.Statements;
            xlatUsingBody();
            Scope = old;
            return u;
        }

        public CodeValueTupleExpression ValueTuple(params CodeExpression[] exprs)
        {
            return new CodeValueTupleExpression(exprs);
        }

        public CodeMemberProperty PropertyDef(string name, Action generatePropertyGetter, Action generatePropertySetter)
        {
            CodeMemberProperty prop = new CodeMemberProperty
            {
                Name = name,
                Attributes = MemberAttributes.Public,
                PropertyType = new CodeTypeReference(typeof(object))
            };
            CodeMemberProperty mem = new CodeMemberProperty();
            List<CodeStatement> old = Scope;
            CodeMember oldMethod = CurrentMember;
            List<CodeStatement> oldStatements = CurrentMemberStatements;
            List<CodeCommentStatement> oldComments = CurrentMemberComments;

            SetCurrentPropertyAccessor(prop, prop.GetStatements);
            Scope = prop.GetStatements;
            generatePropertyGetter();
            if (generatePropertySetter != null)
            {
                SetCurrentPropertyAccessor(prop, prop.SetStatements);
                Scope = prop.SetStatements;
                generatePropertySetter();
            }

            AddMemberWithComments(prop);
            Scope = old;
            CurrentMember = oldMethod;
            CurrentMemberStatements = oldStatements;
            CurrentMemberComments = oldComments;

            return prop;
        }

        public CodeLetClause Let(CodeExpression id, CodeExpression value)
        {
            return new CodeLetClause(id, value);
        }

        public CodeWhereClause Where(CodeExpression e)
        {
            return new CodeWhereClause(e);
        }

        public CodeFromClause From(CodeExpression id, CodeExpression collection)
        {
            return new CodeFromClause(id, collection);
        }

        public CodeQueryClause Select(CodeExpression projection)
        {
            return new CodeSelectClause(projection);
        }

        public CodeQueryExpression Query(params CodeQueryClause[] clauses)
        {
            return new CodeQueryExpression(clauses);
        }

        public CodeExpression This()
        {
            return new CodeThisReferenceExpression();
        }
    }
}