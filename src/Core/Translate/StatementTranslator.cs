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

#endregion License

using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using Pytocs.Core.TypeInference;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.Translate
{
    public class StatementTranslator : IStatementVisitor
    {
        private static readonly Dictionary<Op, CsAssignOp> assignOps = new Dictionary<Op, CsAssignOp>
        {
            {Op.Eq, CsAssignOp.Assign},
            {Op.AugAdd, CsAssignOp.AugAdd}
        };

        private bool async;
        private CodeConstructor classConstructor;
        private readonly ClassDef classDef;
        private IEnumerable<CodeAttributeDeclaration> customAttrs;
        private readonly CodeGenerator gen;
        private readonly SymbolGenerator gensym;
        private readonly HashSet<string> globals;
        private Dictionary<Statement, PropertyDefinition> properties;
        private readonly TypeReferenceTranslator types;
        private readonly ExpTranslator xlat;

        public StatementTranslator(ClassDef classDef, TypeReferenceTranslator types, CodeGenerator gen,
            SymbolGenerator gensym, HashSet<string> globals)
        {
            this.classDef = classDef;
            this.types = types;
            this.gen = gen;
            this.gensym = gensym;
            xlat = new ExpTranslator(classDef, types, gen, gensym);
            properties = new Dictionary<Statement, PropertyDefinition>();
            this.globals = globals;
        }

        public void VisitClass(ClassDef c)
        {
            if (VisitDecorators(c))
            {
                return;
            }

            List<string> baseClasses = c.args.Select(a => GenerateBaseClassName(a.defval)).ToList();
            IEnumerable<CodeCommentStatement> comments = ConvertFirstStringToComments(c.body.stmts);
            SymbolGenerator gensym = new SymbolGenerator();
            StatementTranslator stmtXlt = new StatementTranslator(c, types, gen, gensym, new HashSet<string>())
            {
                properties = FindProperties(c.body.stmts)
            };
            CodeTypeDeclaration csClass = gen.Class(
                c.name.Name,
                baseClasses,
                () => GenerateFields(c),
                () => c.body.Accept(stmtXlt));
            csClass.Comments.AddRange(comments);
            if (customAttrs != null)
            {
                csClass.CustomAttributes.AddRange(customAttrs);
                customAttrs = null;
            }
        }

        public void VisitComment(CommentStatement c)
        {
            gen.Comment(c.comment);
        }

        public void VisitTry(TryStatement t)
        {
            CodeTryCatchFinallyStatement tryStmt = gen.Try(
                () => t.body.Accept(this),
                t.exHandlers.Select(eh => GenerateClause(eh)),
                () =>
                {
                    if (t.finallyHandler != null)
                    {
                        t.finallyHandler.Accept(this);
                    }
                });
        }

        public void VisitExec(ExecStatement e)
        {
            List<CodeExpression> args = new List<CodeExpression>
            {
                e.code.Accept(xlat)
            };
            if (e.globals != null)
            {
                args.Add(e.globals.Accept(xlat));
                if (e.locals != null)
                {
                    args.Add(e.locals.Accept(xlat));
                }
            }

            gen.SideEffect(
                gen.Appl(
                    new CodeVariableReferenceExpression("Python_Exec"),
                    args.ToArray()));
        }

        public void VisitExp(ExpStatement e)
        {
            if (e.Expression is AssignExp ass)
            {
                if (ass.Dst is Identifier idDst &&
                    idDst.Name != "__slots__")
                {
                    (CodeTypeReference dt, ISet<string> nmspcs) = types.TranslateTypeOf(idDst);
                    gen.EnsureImports(nmspcs);
                    gensym.EnsureLocalVariable(idDst.Name, dt, false);
                }

                if (ass.Dst is ExpList dstTuple)
                {
                    if (ass.Src is ExpList srcTuple)
                    {
                        EmitTupleToTupleAssignment(dstTuple.Expressions, srcTuple.Expressions);
                    }
                    else
                    {
                        CodeExpression rhsTuple = ass.Src.Accept(xlat);
                        EmitTupleAssignment(dstTuple.Expressions, rhsTuple);
                    }

                    return;
                }

                CodeExpression rhs = ass.Src.Accept(xlat);
                CodeExpression lhs = ass.Dst.Accept(xlat);
                if (gen.CurrentMember != null)
                {
                    if (ass.op == Op.Assign)
                    {
                        gen.Assign(lhs, rhs);
                    }
                    else
                    {
                        gen.SideEffect(e.Expression.Accept(xlat));
                    }
                }
                else
                {
                    if (ass.Dst is Identifier id)
                    {
                        ClassTranslator_GenerateField(id, xlat, ass);
                    }
                    else
                    {
                        EnsureClassConstructor().Statements.Add(
                            new CodeAssignStatement(lhs, rhs));
                    }
                }

                return;
            }

            if (gen.CurrentMember != null)
            {
                CodeExpression ex = e.Expression.Accept(xlat);
                gen.SideEffect(ex);
            }
            else
            {
                CodeExpression ex = e.Expression.Accept(xlat);
                EnsureClassConstructor().Statements.Add(
                    new CodeExpressionStatement(e.Expression.Accept(xlat)));
            }
        }

        public void VisitFor(ForStatement f)
        {
            switch (f.exprs)
            {
                case Identifier id:
                    CodeExpression exp = id.Accept(xlat);
                    CodeExpression v = f.tests.Accept(xlat);
                    gen.Foreach(exp, v, () => f.Body.Accept(this));
                    return;

                case ExpList expList:
                    GenerateForTuple(f, expList.Expressions);
                    return;

                case PyTuple tuple:
                    GenerateForTuple(f, tuple.values);
                    return;

                case AttributeAccess attributeAccess:
                    GenerateForAttributeAccess(f, attributeAccess.Expression);
                    return;
            }

            throw new NotImplementedException();
        }

        public void VisitFuncdef(FunctionDef f)
        {
            if (VisitDecorators(f))
            {
                return;
            }

            MethodGenerator mgen;
            MemberAttributes attrs = 0;

            if (gen.CurrentMember != null)
            {
                //$TODO: C# 7 supports local functions.
                LambdaBodyGenerator lgen = new LambdaBodyGenerator(classDef, f, f.parameters, true, async, types, gen);
                CodeVariableDeclarationStatement def = lgen.GenerateLambdaVariable(f);
                CodeMemberMethod meth = lgen.Generate();
                def.InitExpression = gen.Lambda(
                    meth.Parameters.Select(p => new CodeVariableReferenceExpression(p.ParameterName)).ToArray(),
                    meth.Statements);
                gen.CurrentMemberStatements.Add(def);
                return;
            }

            if (classDef != null)
            {
                // Inside a class; is this a instance method?
                bool hasSelf = f.parameters.Any(p => p.Id != null && p.Id.Name == "self");
                if (hasSelf)
                {
                    // Presence of 'self' says it _is_ an instance method.
                    List<Parameter> adjustedPs = f.parameters.Where(p => p.Id == null || p.Id.Name != "self").ToList();
                    string fnName = f.name.Name;
                    if (fnName == "__init__")
                    {
                        // Magic function __init__ is a ctor.
                        mgen = new ConstructorGenerator(classDef, f, adjustedPs, types, gen);
                    }
                    else
                    {
                        if (f.name.Name == "__str__")
                        {
                            attrs = MemberAttributes.Override;
                            fnName = "ToString";
                        }

                        mgen = new MethodGenerator(classDef, f, fnName, adjustedPs, false, async, types, gen);
                    }
                }
                else
                {
                    mgen = new MethodGenerator(classDef, f, f.name.Name, f.parameters, true, async, types, gen);
                }
            }
            else
            {
                mgen = new MethodGenerator(classDef, f, f.name.Name, f.parameters, true, async, types, gen);
            }

            CodeMemberMethod m = mgen.Generate();
            m.Attributes |= attrs;
            if (customAttrs != null)
            {
                m.CustomAttributes.AddRange(customAttrs);
                customAttrs = null;
            }
        }

        public void VisitIf(IfStatement i)
        {
            CodeConditionStatement ifStmt = gen.If(i.Test.Accept(xlat), () => Xlat(i.Then), () => Xlat(i.Else));
        }

        public void VisitFrom(FromStatement f)
        {
            foreach (AliasedName alias in f.AliasedNames)
            {
                if (f.DottedName != null)
                {
                    IEnumerable<string> total = f.DottedName.segs.Concat(alias.orig.segs)
                        .Select(s => gen.EscapeKeywordName(s.Name));
                    string aliasName;
                    if (alias.alias == null)
                    {
                        aliasName = total.Last();
                    }
                    else
                    {
                        aliasName = alias.alias.Name;
                    }

                    gen.Using(aliasName, string.Join(".", total));
                }
            }
        }

        public void VisitImport(ImportStatement i)
        {
            foreach (AliasedName name in i.names)
            {
                if (name.alias == null)
                {
                    gen.Using(name.orig.ToString());
                }
                else
                {
                    gen.Using(
                        name.alias.Name,
                        string.Join(
                            ".",
                            name.orig.segs.Select(s => gen.EscapeKeywordName(s.Name))));
                }
            }
        }

        public void VisitPass(PassStatement p)
        {
        }

        public void VisitPrint(PrintStatement p)
        {
            CodeExpression e = null;
            if (p.outputStream != null)
            {
                e = p.outputStream.Accept(xlat);
            }
            else
            {
                e = gen.TypeRefExpr("Console");
            }

            e = gen.MethodRef(
                e, p.trailingComma ? "Write" : "WriteLine");
            gen.SideEffect(
                gen.Appl(
                    e,
                    p.args.Select(a => xlat.VisitArgument(a)).ToArray()));
        }

        public void VisitReturn(ReturnStatement r)
        {
            if (r.Expression != null)
            {
                gen.Return(r.Expression.Accept(xlat));
            }
            else
            {
                gen.Return();
            }
        }

        public void VisitRaise(RaiseStatement r)
        {
            if (r.exToRaise != null)
            {
                DataType dt = types.TypeOf(r.exToRaise);
                if (dt is ClassType)
                {
                    // Python allows expressions like
                    //   raise FooError

                    (CodeTypeReference exceptionType, ISet<string> namespaces) = types.Translate(dt);
                    gen.EnsureImports(namespaces);
                    gen.Throw(gen.New(exceptionType));
                }
                else
                {
                    gen.Throw(r.exToRaise.Accept(xlat));
                }
            }
            else
            {
                gen.Throw();
            }
        }

        public void VisitSuite(SuiteStatement s)
        {
            if (s.stmts.Count == 1)
            {
                s.stmts[0].Accept(this);
            }
            else
            {
                foreach (Statement stmt in s.stmts)
                {
                    stmt.Accept(this);
                }
            }
        }

        public void VisitAsync(AsyncStatement a)
        {
            bool oldAsync = async;
            async = true;
            a.Statement.Accept(this);
            async = oldAsync;
        }

        public void VisitAssert(AssertStatement a)
        {
            foreach (Exp test in a.Tests)
            {
                GenerateAssert(test);
            }
        }

        public void VisitBreak(BreakStatement b)
        {
            gen.Break();
        }

        public void VisitContinue(ContinueStatement c)
        {
            gen.Continue();
        }

        public void VisitDel(DelStatement d)
        {
            List<CodeExpression> exprList = d.Expressions.AsList()
                .Select(e => e.Accept(xlat))
                .ToList();
            if (exprList.Count == 1 &&
                exprList[0] is CodeArrayIndexerExpression aref &&
                aref.Indices.Length == 1)
            {
                // del foo[bar] is likely
                // foo.Remove(bar)
                gen.SideEffect(
                    gen.Appl(
                        gen.MethodRef(
                            aref.TargetObject,
                            "Remove"),
                        aref.Indices[0]));
                return;
            }

            CodeVariableReferenceExpression fn = new CodeVariableReferenceExpression("WONKO_del");
            foreach (CodeExpression exp in exprList)
            {
                gen.SideEffect(gen.Appl(fn, exp));
            }
        }

        public void VisitGlobal(GlobalStatement g)
        {
            foreach (Identifier name in g.names)
            {
                globals.Add(name.Name);
            }
        }

        public void VisitNonLocal(NonlocalStatement n)
        {
            gen.Comment("LOCAL " + string.Join(", ", n.names));
        }

        public void VisitWhile(WhileStatement w)
        {
            if (w.Else != null)
            {
                gen.If(
                    w.Test.Accept(xlat),
                    () => gen.DoWhile(
                        () => w.Body.Accept(this),
                        w.Test.Accept(xlat)),
                    () => w.Else.Accept(this));
            }
            else
            {
                gen.While(
                    w.Test.Accept(xlat),
                    () => w.Body.Accept(this));
            }
        }

        public void VisitWith(WithStatement w)
        {
            gen.Using(
                w.items.Select(wi => Translate(wi)),
                () => w.body.Accept(this));
        }

        public void VisitYield(YieldStatement y)
        {
            gen.Yield(y.Expression.Accept(xlat));
        }

        private IEnumerable<CodeMemberField> GenerateFields(ClassDef c)
        {
            DataType ct = types.TypeOf(c.name);
            IOrderedEnumerable<KeyValuePair<string, ISet<Binding>>> fields = ct.Table.table.Where(m => IsField(m.Value))
                .OrderBy(f => f.Key);
            foreach (KeyValuePair<string, ISet<Binding>> field in fields)
            {
                Binding b = field.Value.First();
                (CodeTypeReference fieldType, ISet<string> ns) = types.Translate(b.Type);
                gen.EnsureImports(ns);
                yield return new CodeMemberField(fieldType, field.Key)
                {
                    Attributes = MemberAttributes.Public
                };
            }
        }

        private bool IsField(ISet<Binding> value)
        {
            foreach (Binding b in value)
            {
                if (b.Kind == BindingKind.ATTRIBUTE
                    &&
                    (!b.IsSynthetic || b.References.Count != 0))
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<Statement, PropertyDefinition> FindProperties(List<Statement> stmts)
        {
            Dictionary<string, PropertyDefinition> propdefs = new Dictionary<string, PropertyDefinition>();
            Dictionary<Statement, PropertyDefinition> result = new Dictionary<Statement, PropertyDefinition>();
            foreach (Statement stmt in stmts)
            {
                if (stmt.decorators == null)
                {
                    continue;
                }

                foreach (Decorator decorator in stmt.decorators)
                {
                    if (IsGetterDecorator(decorator))
                    {
                        FunctionDef def = (FunctionDef)stmt;
                        PropertyDefinition propdef = EnsurePropertyDefinition(propdefs, def);
                        result[stmt] = propdef;
                        propdef.Getter = stmt;
                        propdef.GetterDecoration = decorator;
                    }

                    if (IsSetterDecorator(decorator))
                    {
                        FunctionDef def = (FunctionDef)stmt;
                        PropertyDefinition propdef = EnsurePropertyDefinition(propdefs, def);
                        result[stmt] = propdef;
                        propdef.Setter = stmt;
                        propdef.SetterDecoration = decorator;
                    }
                }
            }

            return result;
        }

        private static PropertyDefinition EnsurePropertyDefinition(Dictionary<string, PropertyDefinition> propdefs,
            FunctionDef def)
        {
            if (!propdefs.TryGetValue(def.name.Name, out PropertyDefinition propdef))
            {
                propdef = new PropertyDefinition(def.name.Name);
                propdefs.Add(def.name.Name, propdef);
            }

            return propdef;
        }

        private static bool IsGetterDecorator(Decorator decoration)
        {
            return decoration.className.segs.Count == 1 &&
                   decoration.className.segs[0].Name == "property";
        }

        private static bool IsSetterDecorator(Decorator decorator)
        {
            if (decorator.className.segs.Count != 2)
            {
                return false;
            }

            return decorator.className.segs[1].Name == "setter";
        }

        public static IEnumerable<CodeCommentStatement> ConvertFirstStringToComments(List<Statement> statements)
        {
            CodeCommentStatement[] nothing = new CodeCommentStatement[0];
            int i = 0;
            for (; i < statements.Count; ++i)
            {
                if (statements[i] is SuiteStatement ste)
                {
                    if (!(ste.stmts[0] is CommentStatement))
                    {
                        break;
                    }
                }
            }

            if (i >= statements.Count)
            {
                return nothing;
            }

            SuiteStatement suiteStmt = statements[i] as SuiteStatement;
            if (suiteStmt == null)
            {
                return nothing;
            }

            ExpStatement expStm = suiteStmt.stmts[0] as ExpStatement;
            if (expStm == null)
            {
                return nothing;
            }

            Str str = expStm.Expression as Str;
            if (str == null)
            {
                return nothing;
            }

            statements.RemoveAt(i);
            return str.s.Replace("\r\n", "\n").Split('\r', '\n').Select(line => new CodeCommentStatement(" " + line));
        }

        private CodeCatchClause GenerateClause(ExceptHandler eh)
        {
            if (eh.type is Identifier ex)
            {
                return gen.CatchClause(
                    null,
                    new CodeTypeReference(ex.Name),
                    () => eh.body.Accept(this));
            }

            return gen.CatchClause(
                null,
                null,
                () => eh.body.Accept(this));
        }

        private string GenerateBaseClassName(Exp exp)
        {
            return exp.ToString();
        }

        private void EmitTupleAssignment(List<Exp> lhs, CodeExpression rhs)
        {
            if (lhs.Any(e => e is StarExp))
            {
                EmitStarredTupleAssignments(lhs, rhs);
            }
            else
            {
                CodeVariableReferenceExpression tup = GenSymLocalTuple();
                gen.Assign(tup, rhs);
                EmitTupleFieldAssignments(lhs, tup);
            }
        }

        /// <summary>
        ///     Translate a starred target by first emitting assignments for
        ///     all non-starred targets, then collecting the remainder in
        ///     the starred target.
        /// </summary>
        private void EmitStarredTupleAssignments(List<Exp> lhs, CodeExpression rhs)
        {
            //$TODO: we don't handle (a, *b, c, d) = ... yet. Who writes code like that?
            gen.EnsureImport("System.Linq");

            CodeVariableReferenceExpression tmp = GenSymLocalIterator();
            gen.Scope.Add(new CodeVariableDeclarationStatement("var", tmp.Name)
            {
                InitExpression = rhs
            });
            for (int index = 0; index < lhs.Count; ++index)
            {
                Exp target = lhs[index];
                if (target is StarExp sTarget)
                {
                    CodeExpression lvalue = sTarget.e.Accept(xlat);
                    CodeExpression rvalue = gen.ApplyMethod(tmp, "Skip", gen.Prim(index));
                    rvalue = gen.ApplyMethod(rvalue, "ToList");
                    gen.Assign(lvalue, rvalue);
                    return;
                }

                if (target != null && target.Name != "_")
                {
                    CodeExpression lvalue = target.Accept(xlat);
                    CodeExpression rvalue = gen.ApplyMethod(tmp, "Element", gen.Prim(index));
                    gen.Assign(lvalue, rvalue);
                }
            }
        }

        private void EmitTupleToTupleAssignment(List<Exp> dstTuple, List<Exp> srcTuple)
        {
            //$TODO cycle detection
            foreach (var pyAss in dstTuple.Zip(srcTuple, (a, b) => new { Dst = a, Src = b }))
            {
                if (pyAss.Dst is Identifier id)
                {
                    gensym.EnsureLocalVariable(id.Name, gen.TypeRef("object"), false);
                }

                gen.Assign(pyAss.Dst.Accept(xlat), pyAss.Src.Accept(xlat));
            }
        }

        private void EmitTupleFieldAssignments(List<Exp> lhs, CodeVariableReferenceExpression tup)
        {
            int i = 0;
            foreach (Exp value in lhs)
            {
                ++i;
                if (value == null || value.Name == "_")
                {
                    continue;
                }

                CodeExpression tupleField = gen.Access(tup, "Item" + i);
                if (value is Identifier id)
                {
                    gensym.EnsureLocalVariable(id.Name, new CodeTypeReference(typeof(object)), false);
                    gen.Assign(new CodeVariableReferenceExpression(id.Name), tupleField);
                }
                else
                {
                    CodeExpression dst = value.Accept(xlat);
                    gen.Assign(dst, tupleField);
                }
            }
        }

        private CodeVariableReferenceExpression GenSymLocalTuple()
        {
            return gensym.GenSymLocal("_tup_", new CodeTypeReference(typeof(object)));
        }

        private CodeVariableReferenceExpression GenSymLocalIterator()
        {
            return gensym.GenSymLocal("_it_", new CodeTypeReference(typeof(object)));
        }

        public CodeVariableReferenceExpression GenSymParameter(string prefix, CodeTypeReference type)
        {
            return gensym.GenSymAutomatic(prefix, type, true);
        }

        private CodeConstructor EnsureClassConstructor()
        {
            if (classConstructor == null)
            {
                classConstructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Static
                };
                gen.CurrentType.Members.Add(classConstructor);
            }

            return classConstructor;
        }

        private void ClassTranslator_GenerateField(Identifier id, ExpTranslator xlat, AssignExp ass)
        {
            if (id.Name == "__slots__")
            {
                // We should already have analyzed the slots in
                // the type inference phase, so we ignore __slots__.
            }
            else
            {
                (CodeTypeReference fieldType, ISet<string> nmspcs) = types.TranslateTypeOf(id);
                gen.EnsureImports(nmspcs);

                GenerateField(id.Name, fieldType, ass.Src.Accept(xlat));
            }
        }

        protected virtual CodeMemberField GenerateField(string name, CodeTypeReference type, CodeExpression value)
        {
            CodeMemberField field = gen.Field(type, name);
            if (value != null)
            {
                field.InitExpression = value;
            }

            return field;
        }

        private void GenerateForAttributeAccess(ForStatement f, Exp id)
        {
            CodeVariableReferenceExpression localVar = gensym.GenSymLocal("_tmp_", gen.TypeRef("object"));
            CodeExpression exp = f.exprs.Accept(xlat);
            CodeExpression v = f.tests.Accept(xlat);
            gen.Foreach(localVar, v, () =>
            {
                gen.Assign(exp, localVar);
                f.Body.Accept(this);
            });
        }

        private void GenerateForTuple(ForStatement f, List<Exp> ids)
        {
            CodeVariableReferenceExpression localVar = GenSymLocalTuple();
            CodeExpression v = f.tests.Accept(xlat);
            gen.Foreach(localVar, v, () =>
            {
                EmitTupleFieldAssignments(ids, localVar);
                f.Body.Accept(this);
            });
        }

        public void Xlat(Statement stmt)
        {
            if (stmt != null)
            {
                stmt.Accept(this);
            }
        }

        private void GenerateAssert(Exp test)
        {
            gen.SideEffect(
                gen.Appl(
                    gen.MethodRef(
                        gen.TypeRefExpr("Debug"),
                        "Assert"),
                    test.Accept(xlat)));
            gen.EnsureImport("System.Diagnostics");
        }

        /// <summary>
        ///     Processes the decorators of <paramref name="stmt" />.
        /// </summary>
        /// <param name="stmt"></param>
        /// <returns>
        ///     If true, the body of the statement has been
        ///     translated, so don't do it again.
        /// </returns>
        public bool VisitDecorators(Statement stmt)
        {
            if (stmt.decorators == null)
            {
                return false;
            }

            List<Decorator> decorators = stmt.decorators;
            if (properties.TryGetValue(stmt, out PropertyDefinition propdef))
            {
                if (propdef.IsTranslated)
                {
                    return true;
                }

                decorators.Remove(propdef.GetterDecoration);
                decorators.Remove(propdef.SetterDecoration);
                customAttrs = decorators.Select(dd => VisitDecorator(dd));
                CodeMemberProperty prop = gen.PropertyDef(
                    propdef.Name,
                    () => GeneratePropertyGetter(propdef.Getter),
                    () => GeneratePropertySetter(propdef.Setter));
                LocalVariableGenerator.Generate(null, prop.GetStatements, globals);
                LocalVariableGenerator.Generate(
                    new List<CodeParameterDeclarationExpression>
                    {
                        new CodeParameterDeclarationExpression(prop.PropertyType, "value")
                    },
                    prop.SetStatements,
                    globals);
                propdef.IsTranslated = true;
                return true;
            }

            customAttrs = stmt.decorators.Select(dd => VisitDecorator(dd));
            return false;
        }

        private void GeneratePropertyGetter(Statement getter)
        {
            FunctionDef def = (FunctionDef)getter;
            MethodGenerator mgen = new MethodGenerator(classDef, def, null, def.parameters, false, async, types, gen);
            IEnumerable<CodeCommentStatement> comments = ConvertFirstStringToComments(def.body.stmts);
            gen.CurrentMemberComments.AddRange(comments);
            mgen.Xlat(def.body);
        }

        private void GeneratePropertySetter(Statement setter)
        {
            if (setter == null)
            {
                return;
            }

            FunctionDef def = (FunctionDef)setter;
            MethodGenerator mgen = new MethodGenerator(classDef, def, null, def.parameters, false, async, types, gen);
            IEnumerable<CodeCommentStatement> comments = ConvertFirstStringToComments(def.body.stmts);
            gen.CurrentMemberComments.AddRange(comments);
            mgen.Xlat(def.body);
        }

        public CodeAttributeDeclaration VisitDecorator(Decorator d)
        {
            return gen.CustomAttr(
                gen.TypeRef(d.className.ToString()),
                d.arguments.Select(a => new CodeAttributeArgument
                {
                    Name = a.name?.ToString(),
                    Value = a.defval?.Accept(xlat)
                }).ToArray());
        }

        private CodeStatement Translate(WithItem wi)
        {
            CodeExpression e1 = wi.t.Accept(xlat);
            CodeExpression e2 = wi.e?.Accept(xlat);
            if (e2 != null)
            {
                return new CodeAssignStatement(e2, e1);
            }

            return new CodeExpressionStatement(e1);
        }
    }

    public class PropertyDefinition
    {
        public Statement Getter;
        public Decorator GetterDecoration;
        public bool IsTranslated;
        public string Name;
        public Statement Setter;
        public Decorator SetterDecoration;

        public PropertyDefinition(string name)
        {
            Name = name;
        }
    }
}