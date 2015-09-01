#region License
//  Copyright 2015 John Källén
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

using Pytocs.CodeModel;
using Pytocs.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    public class StatementTranslator : IStatementVisitor
    {
        private CodeGenerator gen;
        private ExpTranslator xlat;
        private ClassDef currentClass;
        private Dictionary<string, Tuple<string, CodeTypeReference, bool>> autos;

        public StatementTranslator(CodeGenerator gen, Dictionary<string, Tuple<string,CodeTypeReference,bool>> autos)
        {
            this.gen = gen;
            this.autos = autos;
            this.xlat = new ExpTranslator(gen);
        }

        public void VisitClass(ClassDef c)
        {
            var baseClasses = c.args.Select(a => GenerateBaseClassName(a)).ToList();
            var comments = ConvertFirstStringToComments(c.body.stmts);
            var stmtXlt = new StatementTranslator(gen, new Dictionary<string, Tuple<string, CodeTypeReference, bool>>());
            stmtXlt.currentClass = c;
            var csClass = gen.Class(c.name.Name, baseClasses, () => c.body.Accept(stmtXlt));
            csClass.Comments.AddRange(comments);
            if (customAttrs != null)
            {
                csClass.CustomAttributes.AddRange(customAttrs);
                customAttrs = null;
            }
        }

        public static IEnumerable<CodeCommentStatement> ConvertFirstStringToComments(List<Statement> statements)
        {
            var nothing = new CodeCommentStatement[0];
            if (statements.Count == 0)
                return nothing;
            var suiteStmt = statements[0] as SuiteStatement;
            if (suiteStmt == null)
                return nothing;
            var expStm  = suiteStmt.stmts[0] as ExpStatement;
            if (expStm == null)
                return nothing;
            var str = expStm.Expression as Str;
            if (str == null)
                return nothing;
            statements.RemoveAt(0);
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
            var ex = eh.type as Identifier;
            if (ex != null)
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
            throw new NotImplementedException();
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

        private  IEnumerable<CodeAttributeDeclaration> customAttrs;
        private CodeConstructor classConstructor;

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
            var ass = e.Expression as AssignExp;
            if (ass != null)
            {
                var idDst = ass.Dst as Identifier;
                if (idDst != null)
                    EnsureLocalVariable(idDst.Name, new CodeTypeReference(typeof(object)), false);

                var rhs = ass.Src.Accept(xlat);
                var dstTuple = ass.Dst as ExpList;
                if (dstTuple != null)
                {
                    EmitTupleAssignment(dstTuple, rhs);
                    return;
                }
                var lhs = ass.Dst.Accept(xlat);
                if (gen.CurrentMethod != null)
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
                    var id = ass.Dst as Identifier;
                    if (id != null)
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
            if (gen.CurrentMethod != null)
            {
                var ex = e.Expression.Accept(xlat);
                gen.SideEffect(ex);
            }
            else
            {
                var ex = e.Expression.Accept(xlat);
                EnsureClassConstructor().Statements.Add(
                    new CodeExpressionStatement( e.Expression.Accept(xlat)));
            }
        }

        private void EmitTupleAssignment(ExpList lhs, CodeExpression rhs)
        {
            var  tup = GenSymLocal("_tup_", new CodeTypeReference(typeof(object)));
            gen.Assign(tup, rhs);
            int i = 0;
            foreach (Exp value in lhs.Expressions)
            {
                ++i;
                if (value == null || value.Name == "_")
                    continue;
                var tupleField = gen.Access(tup, "Item" + i);
                var id = value as Identifier;
                if (id != null)
                {
                    EnsureLocalVariable(id.Name, new CodeTypeReference(typeof(object)), false);
                    gen.Assign(new CodeVariableReferenceExpression(id.Name), tupleField);
                }
                else
                {
                    var dst = value.Accept(xlat);
                    gen.Assign(dst, tupleField);
                }
            }
        }

        public CodeVariableReferenceExpression GenSymParameter(string prefix, CodeTypeReference type)
        {
            return GenSymAutomatic(prefix, type, true);
        }

        public CodeVariableReferenceExpression GenSymLocal(string prefix, CodeTypeReference type)
        {
            return GenSymAutomatic(prefix, type, false);
        }

        public CodeVariableReferenceExpression GenSymAutomatic(string prefix,  CodeTypeReference type, bool parameter)
        {
            int i = 1;
            while (autos.Select(l => l.Key).Contains(prefix + i))
                ++i;
            EnsureLocalVariable(prefix + i, type, parameter);
            return new CodeVariableReferenceExpression(prefix + i);
        }

        private void EnsureLocalVariable(string name, CodeTypeReference type, bool parameter)
        {
            if (!autos.ContainsKey(name))
                autos.Add(name, Tuple.Create(name, type, parameter));
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
            var slotNames = ass.Src as PyList;
            if (id.Name == "__slots__")
            {
                foreach (var slotName in slotNames.elts.OfType<Str>())
                {
                    GenerateField(slotName.s, null);
                }
            }
            else
            {
                GenerateField(id.Name, ass.Src.Accept(xlat));
            }
        }

        protected virtual CodeMemberField GenerateField(string name, CodeExpression value)
        {
            var field = gen.Field(name);
            if (value != null)
            {
                field.InitExpression = value;
            }
            return field;
        }

        public void VisitFor(ForStatement f)
        {
            var exp = f.exprs.Accept(xlat);
            var v = f.tests.Accept(xlat);
            gen.Foreach(exp, v, () => f.Body.Accept(this));
        }

        public void VisitFuncdef(FunctionDef f)
        {
            MethodGenerator mgen;
            MemberAttributes attrs = 0;

            if (currentClass != null)
            {
                // Inside a class; is this a instance method?
                bool hasSelf = f.parameters.Where(p => p.Id != null && p.Id.Name == "self").Count() > 0;
                if (hasSelf)
                {
                    // Presence of 'self' says it _is_ an instance method.
                    var adjustedPs = f.parameters.Where(p => p.Id == null || p.Id.Name != "self").ToList();
                    var fnName = f.name.Name;
                    if (fnName == "__init__")
                    {
                        // Magic function __init__ is a ctor.
                        mgen = new ConstructorGenerator(f, adjustedPs, gen);
                    }
                    else
                    {
                        if (f.name.Name == "__str__")
                        {
                            attrs = MemberAttributes.Override;
                            fnName = "ToString";
                        }
                        mgen = new MethodGenerator(f, fnName, adjustedPs, false, gen);
                    }
                }
                else
                {
                    mgen = new MethodGenerator(f, f.name.Name, f.parameters, true, gen);
                }
            }
            else
            {
                mgen = new MethodGenerator(f, f.name.Name, f.parameters, true, gen);
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
                    gen.Using(alias.alias.Name, string.Join(".", total));
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
                            name.orig.segs.Concat(new[] { name.alias })
                                .Select(s => gen.EscapeKeywordName(s.Name))));
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
            CodeExpression e= null;
            if (p.outputStream != null)
            {
                e = p.outputStream.Accept(xlat);
            }
            else
            {
                e = new CodeTypeReferenceExpression("Console");
            }
            e = new CodeMethodReferenceExpression(
                e, "WriteLine");
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
                gen.Throw(r.exToRaise.Accept(xlat));
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
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression("Debug"),
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

        public void VisitDecorated(Decorated d)
        {
            this.customAttrs = d.Decorations.Select(dd => VisitDecorator(dd));
            d.Statement.Accept(this);
        }

        public CodeAttributeDeclaration VisitDecorator(Decorator d)
        {
            return gen.CustomAttr(
                gen.TypeRef(d.className.ToString()),
                d.arguments.Select(a => new CodeAttributeArgument
                {
                     Name = a.name != null ? a.name.ToString() : null,
                     Value = a.defval != null ? a.defval.Accept(xlat) : null,
                }).ToArray());
        }

        public void VisitDel(DelStatement d)
        {
            var exprList = d.Expressions.AsList();
            var fn = new CodeVariableReferenceExpression("WONKO_del");
            foreach (var e in exprList)
            {
                gen.SideEffect(gen.Appl(fn, e.Accept(xlat)));
            }
        }

        public void VisitGlobal(GlobalStatement g)
        {
            gen.Comment("GLOBAL " + string.Join(", ", g.names));
        }

        public void VisitNonLocal(NonlocalStatement n)
        {
            throw new NotImplementedException();
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
            CodeExpression e2 = wi.e != null ? wi.e.Accept(xlat) : null;
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
}
