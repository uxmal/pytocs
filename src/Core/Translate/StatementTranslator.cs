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
        private ClassDef classDef;
        private TypeReferenceTranslator types;
        private CodeGenerator gen;
        private ExpTranslator xlat;
        private SymbolGenerator gensym;
        private IEnumerable<CodeAttributeDeclaration> customAttrs;
        private Dictionary<Statement, PropertyDefinition> properties;
        private HashSet<string> globals;
        private CodeConstructor classConstructor;
        private bool async;

        public StatementTranslator(ClassDef classDef, TypeReferenceTranslator types, CodeGenerator gen, SymbolGenerator gensym, HashSet<string> globals)
        {
            this.classDef = classDef;
            this.types = types;
            this.gen = gen;
            this.gensym = gensym;
            this.xlat = new ExpTranslator(classDef, types, gen, gensym);
            this.properties = new Dictionary<Statement, PropertyDefinition>();
            this.globals = globals;
        }

        public void VisitClass(ClassDef c)
        {
            if (VisitDecorators(c))
                return;
            var baseClasses = c.args.Select(a => GenerateBaseClassName(a.defval)).ToList();
            var comments = ConvertFirstStringToComments(c.body.stmts);
            var gensym = new SymbolGenerator();
            var stmtXlt = new StatementTranslator(c, types, gen, gensym, new HashSet<string>());
            stmtXlt.properties = FindProperties(c.body.stmts);
            var csClass = gen.Class(
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

        private IEnumerable<CodeMemberField> GenerateFields(ClassDef c)
        {
            var ct = types.TypeOf(c.name);
            var fields = ct.Table.table.Where(m => IsField(m.Value))
                .OrderBy(f => f.Key);
            foreach (var field in fields)
            {
                var b = field.Value.First();
                var (fieldType, ns) = types.Translate(b.Type);
                gen.EnsureImports(ns);
                yield return new CodeMemberField(fieldType, field.Key)
                {
                    Attributes = MemberAttributes.Public
                };
            }
        }

        private bool IsField(ISet<Binding> value)
        {
            foreach (var b in value)
            {
                if (b.Kind == BindingKind.ATTRIBUTE
                    &&
                   (!b.IsSynthetic || b.References.Count != 0))
                    return true;
            }
            return false;
        }

        public Dictionary<Statement, PropertyDefinition> FindProperties(List<Statement> stmts)
        {
            var propdefs = new Dictionary<string, PropertyDefinition>();
            var result = new Dictionary<Statement, PropertyDefinition>();
            foreach (var stmt in stmts)
            {
                if (stmt.decorators == null)
                    continue;
                foreach (var decorator in stmt.decorators)
                {
                    if (IsGetterDecorator(decorator))
                    {
                        var def = (FunctionDef)stmt;
                        var propdef = EnsurePropertyDefinition(propdefs, def);
                        result[stmt] = propdef;
                        propdef.Getter = stmt;
                        propdef.GetterDecoration = decorator;
                    }
                    if (IsSetterDecorator(decorator))
                    {
                        var def = (FunctionDef)stmt;
                        var propdef = EnsurePropertyDefinition(propdefs, def);
                        result[stmt] = propdef;
                        propdef.Setter = stmt;
                        propdef.SetterDecoration = decorator;
                    }
                }
            }
            return result;
        }

        private static PropertyDefinition EnsurePropertyDefinition(Dictionary<string, PropertyDefinition> propdefs, FunctionDef def)
        {
            if (!propdefs.TryGetValue(def.name.Name, out var propdef))
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
                return false;
            return decorator.className.segs[1].Name == "setter";
        }

        public static IEnumerable<CodeCommentStatement> ConvertFirstStringToComments(List<Statement> statements)
        {
            var nothing = new CodeCommentStatement[0];
            int i = 0;
            for (; i < statements.Count; ++i)
            {
                if (statements[i] is SuiteStatement ste)
                {
                    if (!(ste.stmts[0] is CommentStatement))
                        break;
                }
            }
            if (i >= statements.Count)
                return nothing;
            var suiteStmt = statements[i] as SuiteStatement;
            if (suiteStmt == null)
                return nothing;
            var expStm = suiteStmt.stmts[0] as ExpStatement;
            if (expStm == null)
                return nothing;
            var str = expStm.Expression as Str;
            if (str == null)
                return nothing;
            statements.RemoveAt(i);
            return str.s.Replace("\r\n", "\n").Split('\r', '\n').Select(line => new CodeCommentStatement(" " + line));
        }

        public void VisitComment(CommentStatement c)
        {
            gen.Comment(c.comment);
        }

        public void VisitTry(TryStatement t)
        {
            var tryStmt = gen.Try(
                () => t.body.Accept(this),
                t.exHandlers.Select(eh => GenerateClause(eh)),
                () =>
                {
                    if (t.finallyHandler != null)
                        t.finallyHandler.Accept(this);
                });
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
            else
            {
                return gen.CatchClause(
                    null,
                    null,
                    () => eh.body.Accept(this));
            }
        }

        private string GenerateBaseClassName(Exp exp)
        {
            return exp.ToString();
        }

        private static Dictionary<Op, CsAssignOp> assignOps = new Dictionary<Op, CsAssignOp>()
        {
            { Op.Eq, CsAssignOp.Assign },
            { Op.AugAdd, CsAssignOp.AugAdd },
        };

        public void VisitExec(ExecStatement e)
        {
            var args = new List<CodeExpression>();
            args.Add(e.code.Accept(xlat));
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
                    var (dt, nmspcs) = types.TranslateTypeOf(idDst);
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
                        var rhsTuple = ass.Src.Accept(xlat);
                        EmitTupleAssignment(dstTuple.Expressions, rhsTuple);
                    }
                    return;
                }
                var rhs = ass.Src.Accept(xlat);
                var lhs = ass.Dst.Accept(xlat);
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
                var ex = e.Expression.Accept(xlat);
                gen.SideEffect(ex);
            }
            else
            {
                var ex = e.Expression.Accept(xlat);
                EnsureClassConstructor().Statements.Add(
                    new CodeExpressionStatement(e.Expression.Accept(xlat)));
            }
        }

        private void EmitTupleAssignment(List<Exp> lhs, CodeExpression rhs)
        {
            if (lhs.Any(e => e is StarExp))
            {
                EmitStarredTupleAssignments(lhs, rhs);
            }
            else
            {
                var tup = GenSymLocalTuple();
                gen.Assign(tup, rhs);
                EmitTupleFieldAssignments(lhs, tup);
            }
        }

        /// <summary>
        /// Translate a starred target by first emitting assignments for
        /// all non-starred targets, then collecting the remainder in
        /// the starred target.
        /// </summary>
        private void EmitStarredTupleAssignments(List<Exp> lhs, CodeExpression rhs)
        {
            //$TODO: we don't handle (a, *b, c, d) = ... yet. Who writes code like that?
            gen.EnsureImport("System.Linq");

            var tmp = GenSymLocalIterator();
            gen.Scope.Add(new CodeVariableDeclarationStatement("var", tmp.Name)
            {
                InitExpression = rhs
            });
            for (int index = 0; index < lhs.Count; ++index)
            {
                var target = lhs[index];
                if (target is StarExp sTarget)
                {
                    var lvalue = sTarget.e.Accept(xlat);
                    var rvalue = gen.ApplyMethod(tmp, "Skip", gen.Prim(index));
                    rvalue = gen.ApplyMethod(rvalue, "ToList");
                    gen.Assign(lvalue, rvalue);
                    return;
                }
                else if (target != null && target.Name != "_")
                {
                    var lvalue = target.Accept(xlat);
                    var rvalue = gen.ApplyMethod(tmp, "Element", gen.Prim(index));
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
                    continue;
                var tupleField = gen.Access(tup, "Item" + i);
                if (value is Identifier id)
                {
                    gensym.EnsureLocalVariable(id.Name, new CodeTypeReference(typeof(object)), false);
                    gen.Assign(new CodeVariableReferenceExpression(id.Name), tupleField);
                }
                else
                {
                    var dst = value.Accept(xlat);
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
            if (this.classConstructor == null)
            {
                this.classConstructor = new CodeConstructor
                {
                    Attributes = MemberAttributes.Static,
                };
                gen.CurrentType.Members.Add(classConstructor);
            }
            return this.classConstructor;
        }

        private void ClassTranslator_GenerateField(Identifier id, ExpTranslator xlat, AssignExp ass)
        {
            if (id.Name == "__slots__")
            {
                // We should already have analyzed the slots in
                // the type inference phase, so we ignore __slots__.
                return;
            }
            else
            {
                var (fieldType, nmspcs) = types.TranslateTypeOf(id);
                gen.EnsureImports(nmspcs);

                GenerateField(id.Name, fieldType, ass.Src.Accept(xlat));
            }
        }

        protected virtual CodeMemberField GenerateField(string name, CodeTypeReference type, CodeExpression value)
        {
            var field = gen.Field(type, name);
            if (value != null)
            {
                field.InitExpression = value;
            }
            return field;
        }

        public void VisitFor(ForStatement f)
        {
            switch (f.exprs)
            {
                case Identifier id:
                    var exp = id.Accept(xlat);
                    var v = f.tests.Accept(xlat);
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

        private void GenerateForAttributeAccess(ForStatement f, Exp id)
        {
            var localVar = gensym.GenSymLocal("_tmp_", gen.TypeRef("object"));
            var exp = f.exprs.Accept(xlat);
            var v = f.tests.Accept(xlat);
            gen.Foreach(localVar, v, () =>
            {
                gen.Assign(exp, localVar);
                f.Body.Accept(this);
            });
        }

        private void GenerateForTuple(ForStatement f, List<Exp> ids)
        {
            var localVar = GenSymLocalTuple();
            var v = f.tests.Accept(xlat);
            gen.Foreach(localVar, v, () =>
            {
                EmitTupleFieldAssignments(ids, localVar);
                f.Body.Accept(this);
            });
        }

        public void VisitFuncdef(FunctionDef f)
        {
            if (VisitDecorators(f))
                return;
            MethodGenerator mgen;
            MemberAttributes attrs = 0;

            if (this.gen.CurrentMember != null)
            {
                //$TODO: C# 7 supports local functions.
                var lgen = new LambdaBodyGenerator(classDef, f, f.parameters, true, async, types, gen);
                var def = lgen.GenerateLambdaVariable(f);
                var meth = lgen.Generate();
                def.InitExpression = gen.Lambda(
                    meth.Parameters.Select(p => new CodeVariableReferenceExpression(p.ParameterName)).ToArray(),
                    meth.Statements);
                gen.CurrentMemberStatements.Add(def);
                return;
            }
            if (this.classDef != null)
            {
                // Inside a class; is this a instance method?
                bool hasSelf = f.parameters.Any(p => p.Id != null && p.Id.Name == "self");
                if (hasSelf)
                {
                    // Presence of 'self' says it _is_ an instance method.
                    var adjustedPs = f.parameters.Where(p => p.Id == null || p.Id.Name != "self").ToList();
                    var fnName = f.name.Name;
                    if (fnName == "__init__")
                    {
                        // Magic function __init__ is a ctor.
                        mgen = new ConstructorGenerator(this.classDef, f, adjustedPs, types, gen);
                    }
                    else
                    {
                        if (f.name.Name == "__str__")
                        {
                            attrs = MemberAttributes.Override;
                            fnName = "ToString";
                        }
                        mgen = new MethodGenerator(this.classDef, f, fnName, adjustedPs, false, async, types, gen);
                    }
                }
                else
                {
                    mgen = new MethodGenerator(this.classDef, f, f.name.Name, f.parameters, true, async, types, gen);
                }
            }
            else
            {
                mgen = new MethodGenerator(this.classDef, f, f.name.Name, f.parameters, true, async, types, gen);
            }
            CodeMemberMethod m = mgen.Generate();
            m.Attributes |= attrs;
            if (customAttrs != null)
            {
                m.CustomAttributes.AddRange(this.customAttrs);
                customAttrs = null;
            }
        }

        public void VisitIf(IfStatement i)
        {
            var ifStmt = gen.If(i.Test.Accept(xlat), () => Xlat(i.Then), () => Xlat(i.Else));
        }

        public void VisitFrom(FromStatement f)
        {
            foreach (var alias in f.AliasedNames)
            {
                if (f.DottedName != null)
                {
                    var total = f.DottedName.segs.Concat(alias.orig.segs)
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
            foreach (var name in i.names)
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

        public void Xlat(Statement stmt)
        {
            if (stmt != null)
            {
                stmt.Accept(this);
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
                gen.Return(r.Expression.Accept(xlat));
            else
                gen.Return();
        }

        public void VisitRaise(RaiseStatement r)
        {
            if (r.exToRaise != null)
            {
                var dt = types.TypeOf(r.exToRaise);
                if (dt is ClassType)
                {
                    // Python allows expressions like
                    //   raise FooError

                    var (exceptionType, namespaces) = types.Translate(dt);
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
                foreach (var stmt in s.stmts)
                {
                    stmt.Accept(this);
                }
            }
        }

        public void VisitAsync(AsyncStatement a)
        {
            var oldAsync = this.async;
            this.async = true;
            a.Statement.Accept(this);
            this.async = oldAsync;
        }

        public void VisitAssert(AssertStatement a)
        {
            foreach (var test in a.Tests)
            {
                GenerateAssert(test);
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

        public void VisitBreak(BreakStatement b)
        {
            gen.Break();
        }

        public void VisitContinue(ContinueStatement c)
        {
            gen.Continue();
        }

        /// <summary>
        /// Processes the decorators of <paramref name="stmt"/>.
        /// </summary>
        /// <param name="stmt"></param>
        /// <returns>If true, the body of the statement has been
        /// translated, so don't do it again.</returns>
        public bool VisitDecorators(Statement stmt)
        {
            if (stmt.decorators == null)
                return false;
            var decorators = stmt.decorators;
            if (this.properties.TryGetValue(stmt, out var propdef))
            {
                if (propdef.IsTranslated)
                    return true;
                decorators.Remove(propdef.GetterDecoration);
                decorators.Remove(propdef.SetterDecoration);
                this.customAttrs = decorators.Select(dd => VisitDecorator(dd));
                var prop = gen.PropertyDef(
                    propdef.Name,
                    () => GeneratePropertyGetter(propdef.Getter),
                    () => GeneratePropertySetter(propdef.Setter));
                LocalVariableGenerator.Generate(null, prop.GetStatements, globals);
                LocalVariableGenerator.Generate(
                    new List<CodeParameterDeclarationExpression> {
                        new CodeParameterDeclarationExpression(prop.PropertyType, "value"),
                    },
                    prop.SetStatements,
                    globals);
                propdef.IsTranslated = true;
                return true;
            }
            else
            {
                this.customAttrs = stmt.decorators.Select(dd => VisitDecorator(dd));
                return false;
            }
        }

        private void GeneratePropertyGetter(Statement getter)
        {
            var def = (FunctionDef)getter;
            var mgen = new MethodGenerator(this.classDef, def, null, def.parameters, false, async, types, gen);
            var comments = ConvertFirstStringToComments(def.body.stmts);
            gen.CurrentMemberComments.AddRange(comments);
            mgen.Xlat(def.body);
        }

        private void GeneratePropertySetter(Statement setter)
        {
            if (setter == null)
                return;
            var def = (FunctionDef)setter;
            var mgen = new MethodGenerator(this.classDef, def, null, def.parameters, false, async, types, gen);
            var comments = ConvertFirstStringToComments(def.body.stmts);
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
                    Value = a.defval?.Accept(xlat),
                }).ToArray());
        }

        public void VisitDel(DelStatement d)
        {
            var exprList = d.Expressions.AsList()
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
            var fn = new CodeVariableReferenceExpression("WONKO_del");
            foreach (var exp in exprList)
            {
                gen.SideEffect(gen.Appl(fn, exp));
            }
        }

        public void VisitGlobal(GlobalStatement g)
        {
            foreach (var name in g.names)
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

        private CodeStatement Translate(WithItem wi)
        {
            CodeExpression e1 = wi.t.Accept(xlat);
            CodeExpression e2 = wi.e?.Accept(xlat);
            if (e2 != null)
                return new CodeAssignStatement(e2, e1);
            else
                return new CodeExpressionStatement(e1);
        }

        public void VisitYield(YieldStatement y)
        {
            gen.Yield(y.Expression.Accept(xlat));
        }
    }

    public class PropertyDefinition
    {
        public string Name;
        public Statement Getter;
        public Statement Setter;
        public Decorator GetterDecoration;
        public Decorator SetterDecoration;
        public bool IsTranslated;

        public PropertyDefinition(string name)
        {
            this.Name = name;
        }
    }
}