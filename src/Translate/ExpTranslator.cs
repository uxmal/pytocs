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

using Pytocs.Syntax;
using Pytocs.CodeModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    /// <summary>
    /// Translates a Python expression.
    /// </summary>
    public class ExpTranslator : IExpVisitor<CodeExpression>
    {
        private static Dictionary<Op, CodeOperatorType> mppyoptocsop = new Dictionary<Op, CodeOperatorType>() 
        {
            { Op.Add, CodeOperatorType.Add },
            { Op.Sub, CodeOperatorType.Sub },
            { Op.Eq, CodeOperatorType.Equal },
            { Op.Ne, CodeOperatorType.NotEqual },
            { Op.Gt, CodeOperatorType.Gt },
            { Op.Ge, CodeOperatorType.Ge },
            { Op.Le, CodeOperatorType.Le },
            { Op.Lt, CodeOperatorType.Lt },
            { Op.LogAnd, CodeOperatorType.LogAnd },
            { Op.BitAnd, CodeOperatorType.BitAnd },
            { Op.LogOr, CodeOperatorType.LogOr },
            { Op.BitOr, CodeOperatorType.BitOr },
            { Op.Not, CodeOperatorType.Not },
            { Op.Mod, CodeOperatorType.Mod },
            { Op.Div, CodeOperatorType.Div },
            { Op.Mul, CodeOperatorType.Mul },
            { Op.Shr, CodeOperatorType.Shr },
            { Op.Shl, CodeOperatorType.Shl },
            { Op.Complement, CodeOperatorType.Complement },
            { Op.IDiv, CodeOperatorType.Div },
            { Op.Xor, CodeOperatorType.BitXor },
            { Op.AugAdd, CodeOperatorType.AddEq },
            { Op.AugSub, CodeOperatorType.SubEq },
            { Op.AugMul, CodeOperatorType.MulEq },
            { Op.AugDiv, CodeOperatorType.DivEq },
            { Op.AugAnd, CodeOperatorType.AndEq },
            { Op.AugOr, CodeOperatorType.OrEq },
            { Op.AugShl, CodeOperatorType.ShlEq },
            { Op.AugShr, CodeOperatorType.ShrEq },
            { Op.AugXor, CodeOperatorType.XorEq },
        };

        private CodeGenerator m;

        public ExpTranslator(CodeGenerator gen)
        {
            this.m = gen;
        }

        public CodeExpression VisitCompFor(CompFor f)
        {
            throw new NotImplementedException();
            //var v = compFor.variable.Accept(this);
            //var c = Translate(v, compFor);
            //var mr = new CodeMethodReferenceExpression(c, "Select");
            //var s = m.Appl(mr, new CodeExpression[] {
            //            m.Lambda(
            //                new CodeExpression[] { v },
            //                a.name.Accept(this))
            //        });
            //return s;
        }

        public CodeExpression VisitCompIf(CompIf i)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitDottedName(DottedName d)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitEllipsis(Ellipsis e)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitExpList(ExpList l)
        {
            var fn = new CodeMethodReferenceExpression(
                new CodeTypeReferenceExpression("Tuple"),
                "Create");
            return m.Appl
                (fn,
                l.Expressions.Select(e => e.Accept(this)).ToArray());
        }

        private IEnumerable<CodeExpression> TranslateArgs(Application args)
        {
            foreach (var arg in args.args)
            {
                yield return VisitArgument(arg);
            }
            foreach (var arg in args.keywords)
            {
                yield return VisitArgument(arg);
            }
            if (args.stargs != null)
                yield return args.stargs.Accept(this);
            if (args.kwargs != null)
                yield return args.kwargs.Accept(this);
        }

        public CodeExpression VisitApplication(Application appl)
        {
            Debug.Print("appl: {0}", appl.fn);
            var fn = appl.fn.Accept(this);
            var args = TranslateArgs(appl).ToArray();
            var id = fn as CodeVariableReferenceExpression;
            if (id == null)
            {
                return m.Appl(appl.fn.Accept(this), args);
            }
            if (id.Name == "isinstance" && appl.args.Count == 2)
            {
                return new CodeBinaryOperatorExpression(
                    appl.args[0].defval.Accept(this),
                    CodeOperatorType.Is,
                    new CodeTypeReferenceExpression(appl.args[1].defval.ToString()));
            }
            if (id.Name == "int")
            {
                m.EnsureImport("System");
                fn = new CodeMethodReferenceExpression(
                    m.TypeRefExpr("Convert"), "ToInt32");
            }
            if (id.Name == "list")
            {
                if (args.Length == 1)
                {
                    m.EnsureImport("System.Linq");
                    fn = new CodeMethodReferenceExpression(args[0], "ToList");
                    return m.Appl(fn);
                }

            }
            return m.Appl(fn, args);
        }

        public CodeExpression VisitArrayRef(ArrayRef aref)
        {
            var target = aref.array.Accept(this);
            var subs = aref.subs
                .Select(s =>
                {
                    if (s.upper != null || s.step != null)
                        return new CodeVariableReferenceExpression(string.Format(
                            "{0}:{1}:{2}",
                            s.lower, s.upper, s.step));
                    if (s.lower != null)
                        return s.lower.Accept(this);
                    else if (s.upper != null)
                        return s.upper.Accept(this);
                    else if (s.step != null)
                        return s.step.Accept(this);
                    else
                        return new CodePrimitiveExpression(":");
                })
                .ToArray();
            return m.Aref(target, subs);
        }

        public CodeExpression VisitArgument(Argument a)
        {
            Debug.Print("  arg {0}", a);
            if (a is ListArgument)
            {
                return m.Appl(
                    new CodeVariableReferenceExpression("__flatten___"),
                    new CodeNamedArgument(a.name.Accept(this), null));
            }
            if (a.name == null)
            {
                Debug.Assert(a.defval != null);
                return a.defval.Accept(this);
            }
            else
            {
                var compFor = a.defval as CompFor;
                if (compFor != null)
                {
                    var v = compFor.variable.Accept(this);
                    var c = Translate(v, compFor);
                    var mr = new CodeMethodReferenceExpression(c, "Select");
                    var s = m.Appl(mr, new CodeExpression[] {
                        m.Lambda(
                            new CodeExpression[] { v },
                            a.name.Accept(this))
                    });
                    return s;
                }
                else
                {
                    return new CodeNamedArgument(
                        a.name.Accept(this),
                        a.defval != null
                            ? a.defval.Accept(this)
                            : null);
                }
            }
        }

        public CodeExpression VisitAssignExp(AssignExp e)
        {
            return m.BinOp(
                e.Dst.Accept(this),
                mppyoptocsop[e.op],
                e.Src.Accept(this));
        }

        public CodeExpression VisitRealLiteral(RealLiteral r)
        {
            return new CodePrimitiveExpression(r.Value);
        }


        public CodeExpression VisitDictInitializer(DictInitializer s)
        {
            var items = s.KeyValues.Select(kv => new CodeInitializerExpression(
                kv.Key.Accept(this),
                kv.Value.Accept(this)));
            m.EnsureImport("System.Collections");
            var init = new CodeObjectCreateExpression
            {
                Type = new CodeTypeReference("Hashtable"),
                Initializer = new CodeInitializerExpression
                {
                    Values = items.ToArray()
                }
            };
            return init;
        }

        public CodeExpression VisitSet(PySet s)
        {
            var items = s.exps.Select(e => new CodeInitializerExpression(e.Accept(this)));
            m.EnsureImport("System.Collections");
            var init = new CodeObjectCreateExpression
            {
                Type = new CodeTypeReference("HashSet"),
                Initializer = new CodeInitializerExpression
                {
                    Values = items.ToArray()
                }
            };
            return init;
        }

        public CodeExpression VisitSetComprehension(SetComprehension sc)
        {
            var compFor = (CompFor) sc.Collection;
            var list = compFor.collection.Accept(this);
            var id = (Identifier)compFor.variable;
            return m.Appl(
                new CodeMethodReferenceExpression(
                    list,
                    "ToHashSet"),
                    m.Lambda(
                        new CodeExpression[] { id.Accept(this) },
                        sc.Projection.Accept(this)));
            //var varList = dc.source.variable as ExpList;
#if NO
            var list = dc.source.collection.Accept(this);
            var varList = dc.source.variable as ExpList;
            if (varList != null)
            {
                if (varList.Expressions.Count != 2)
                    throw new InvalidOperationException("Variable list should contain one or two variables.");
                var k = (Identifier)varList.Expressions[0];
                var v = (Identifier)varList.Expressions[1];
                var kValue = dc.key.Accept(this);
                var vValue = dc.value.Accept(this);
                return m.Appl(
                    new CodeMethodReferenceExpression(
                        list,
                        "ToDictionary"),
                        m.Lambda(new CodeExpression[] { k.Accept(this) }, kValue),
                        m.Lambda(new CodeExpression[] { v.Accept(this) }, vValue));
            }
            var id = sc dc.source.variable as Identifier;
            if (id != null)
            {
                var vValue = dc.value.Accept(this);
                return m.Appl(
                    new CodeMethodReferenceExpression(
                        list,
                        "ToHashSet"),
                    m.Lambda(new CodeExpression[] { id.Accept(this) }, vValue));
            }
            var tuple = dc.source.variable as PyTuple;
            if (tuple != null)
            {
                //TODO: tuples, especially nested tuples, are hard.
                return new CodePrimitiveExpression("!!!{" +
                    dc.key.Accept(this) +
                    ": " +
                    dc.value.Accept(this));
            }
#endif
        }

        public CodeExpression VisitUnary(UnaryExp u)
        {
            var e = u.e.Accept(this);
            return new CodeUnaryOperatorExpression(mppyoptocsop[u.op], e);
        }

        public CodeExpression VisitBinExp(BinExp bin)
        {
            if (bin.op == Op.Mod &&
                (bin.r is PyTuple || bin.l is Str))
            {
                return TranslateFormatExpression(bin.l, bin.r);
            }

            var l = bin.l.Accept(this);
            var r = bin.r.Accept(this);
            CodeOperatorType opDst;
            if (mppyoptocsop.TryGetValue(bin.op, out opDst))
            {
                return m.BinOp(l, opDst, r);
            }
            if (bin.op == Op.Is)
            {
                if (bin.r is NoneExp)
                {
                    return m.BinOp(l, CodeOperatorType.IdentityEquality, r);
                }
            }
            if (bin.op == Op.IsNot)
            {
                return m.BinOp(l, CodeOperatorType.IdentityInequality, r);
            }
            if (bin.op == Op.In)
            {
                return m.Appl(
                    new CodeMethodReferenceExpression(r, "Contains"),
                     l);
            }
            if (bin.op == Op.NotIn)
            {
                return new CodeUnaryOperatorExpression(
                    CodeOperatorType.Not,
                    m.Appl(
                        new CodeMethodReferenceExpression(r, "Contains"),
                        l));
            }
            if (bin.op == Op.Is)
            {
                return m.Appl(
                    new CodeMethodReferenceExpression(
                        new CodeTypeReferenceExpression("object"),
                        "ReferenceEquals"),
                    l,
                    r);
            }
            if (bin.op == Op.Exp)
            {
                return m.Appl(
                    new CodeVariableReferenceExpression("pow"), l, r);
            }
            return m.BinOp(l, mppyoptocsop[bin.op], r);
        }

        private CodeExpression TranslateFormatExpression(Exp formatString, Exp pyArgs)
        {
            var args = new List<CodeExpression>();
            args.Add(formatString.Accept(this));

            var tuple = pyArgs as PyTuple;
            if (tuple != null)
            {
                args.AddRange(tuple.values
                    .Select(e => e.Accept(this)));
            }
            else
            {
                args.Add(pyArgs.Accept(this));
            }
            m.EnsureImport("System");
            return m.Appl(
                new CodeMethodReferenceExpression(
                    new CodeTypeReferenceExpression("String"),
                    "Format"),
                args.ToArray());
        }

        public CodeExpression VisitListComprehension(ListComprehension lc)
        {
            var compFor = (CompFor) lc.Collection;
            var v = compFor.variable.Accept(this);
            var c = Translate(v, compFor);
            var mr = new CodeMethodReferenceExpression(c, "Select");
            var s = m.Appl(mr, new CodeExpression[] {
                m.Lambda(
                    new CodeExpression[] { v },
                    lc.Projection.Accept(this))
            });
            return s;
        }

        public CodeExpression VisitLambda(Lambda l)
        {
            var e = new CodeLambdaExpression(
                l.args.Select(a => a.name.Accept(this)).ToArray(),
                l.body.Accept(this));
            return e;
        }

        private CodeExpression Translate(CodeExpression v, CompFor compFor)
        {
            var c = compFor.collection.Accept(this);
            if (compFor.next != null)
            {
                var filter = compFor.next as CompIf;
                if (filter != null)
                {
                    c = Where(c, v, filter.test.Accept(this));
                }
                var join = compFor.next as CompFor;
                if (join != null)
                {
                    //var pySrc = "((a, s) for a in stackframe.alocs.values() for s in a._segment_list)";
                    //string sExp = "stackframe.alocs.SelectMany(aa => aa._segment_list, (a, s) => Tuple.Create( a, s ))";
                    return m.Appl(
                        new CodeMethodReferenceExpression(c, "SelectMany"),
                        m.Lambda(
                            new CodeExpression[] {((Identifier)compFor.variable).Accept(this) },
                            join.collection.Accept(this)),
                        m.Lambda(
                            new CodeExpression[] { 
                                ((Identifier)compFor.variable).Accept(this),
                                ((Identifier)join.variable).Accept(this)
                            },
                            m.Appl(
                                new CodeMethodReferenceExpression(
                                    new CodeTypeReferenceExpression("Tuple"),
                                    "Create"),
                                ((Identifier)compFor.variable).Accept(this),
                                ((Identifier)join.variable).Accept(this))));
                }
            }
            return c;
        }

        private CodeExpression Where(CodeExpression c, CodeExpression v, CodeExpression filter)
        {
            var mr = new CodeMethodReferenceExpression(c, "Where");
            var f = m.Appl(mr, new CodeExpression[] {
                m.Lambda(
                    new CodeExpression[] { v },
                    filter)
            });
            return f;
        }

        public CodeExpression VisitList(PyList l)
        {
            return m.ListInitializer(
                l.elts
                .Where(e => e != null)
                .Select(e => e.Accept(this)));
        }

        public CodeExpression VisitFieldAccess(AttributeAccess acc)
        {
            var exp = acc.Expression.Accept(this);
            return new CodeFieldReferenceExpression(exp, acc.FieldName.Name);
        }

        public CodeExpression VisitIdentifier(Identifier id)
        {
            if (id.Name == "self")
                return new CodeThisReferenceExpression();
            else
            {
                return new CodeVariableReferenceExpression(id.Name);
            }
        }

        public CodeExpression VisitNoneExp()
        {
            return new CodePrimitiveExpression(null);
        }

        public CodeExpression VisitBooleanLiteral(BooleanLiteral b)
        {
            return new CodePrimitiveExpression(b.Value);
        }

        public CodeExpression VisitImaginary(ImaginaryLiteral im)
        {
            throw new NotSupportedException();
        }

        public CodeExpression VisitIntLiteral(IntLiteral i)
        {
            return new CodePrimitiveExpression(i.Value);
        }

        public CodeExpression VisitLongLiteral(LongLiteral l)
        {
            return new CodePrimitiveExpression(l.Value);
        }

        public CodeExpression VisitStarExp(StarExp s)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitBytes(Bytes b)
        {
            return new CodePrimitiveExpression(b);
        }

        public CodeExpression VisitStr(Str s)
        {
            return new CodePrimitiveExpression(s);
        }

        public CodeExpression VisitTest(TestExp t)
        {
            var cond = t.Condition.Accept(this);
            var cons = t.Consequent.Accept(this);
            var alt = t.Alternative.Accept(this);
            return new CodeConditionExpression(cond, cons, alt);
        }

        public CodeExpression VisitTuple(PyTuple tuple)
        {
            var fn = new CodeMethodReferenceExpression(
                new CodeTypeReferenceExpression("Tuple"), "Create");
            if (tuple.values.Count == 0)
            {
                return m.Appl(fn, new CodePrimitiveExpression("<Empty>"));
            }
            else
            {
                return m.Appl(fn, tuple.values.Select(v => v.Accept(this)).ToArray());
            }
        }

        public CodeExpression VisitYieldExp(YieldExp yieldExp)
        {
            if (yieldExp.exp == null)
                return new CodePrimitiveExpression(null);
            else
                return yieldExp.exp.Accept(this);
        }

        public CodeExpression VisitYieldFromExp(YieldFromExp yieldExp)
        {
            return yieldExp.Expression.Accept(this);
        }

        public CodeExpression VisitAliasedExp(AliasedExp aliasedExp)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitSlice(Slice slice)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitDictComprehension(DictComprehension dc)
        {
            //{ k:copy.copy(v) for k, v in path.info.iteritems() }
            //string sExp = "path.info.iteritems.ToDictionary(k => k, v => copy.copy(v))";

            var list = dc.source.collection.Accept(this);
            var varList = dc.source.variable as ExpList;
            if (varList != null)
            {
                if (varList.Expressions.Count != 2)
                    throw new InvalidOperationException("Variable list should contain one or two variables.");
                var k = (Identifier)varList.Expressions[0];
                var v = (Identifier)varList.Expressions[1];
                var kValue = dc.key.Accept(this);
                var vValue = dc.value.Accept(this);
                return m.Appl(
                    new CodeMethodReferenceExpression(
                        list,
                        "ToDictionary"),
                        m.Lambda(new CodeExpression[] { k.Accept(this) }, kValue),
                        m.Lambda(new CodeExpression[] { v.Accept(this) }, vValue));
            }
            var id = dc.source.variable as Identifier;
            if (id != null)
            { 
                var vValue = dc.value.Accept(this);
                return m.Appl(
                    new CodeMethodReferenceExpression(
                        list,
                        "ToHashSet"),
                    m.Lambda(new CodeExpression[] { id.Accept(this) }, vValue));
            }
            var tuple = dc.source.variable as PyTuple;
            if (tuple != null)
            {
                //TODO: tuples, especially nested tuples, are hard.
                return new CodePrimitiveExpression("!!!{" +
                    dc.key.Accept(this) +
                    ": " +
                    dc.value.Accept(this));
            }

            throw new NotImplementedException();
        }
    }
}