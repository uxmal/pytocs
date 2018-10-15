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

using Pytocs.Syntax;
using Pytocs.CodeModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

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

        internal CodeGenerator m;
        internal SymbolGenerator gensym;
        internal IntrinsicTranslator intrinsic;

        public ExpTranslator(CodeGenerator gen, SymbolGenerator gensym)
        {
            this.m = gen;
            this.gensym = gensym;
            this.intrinsic = new IntrinsicTranslator(this);
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
            var fn = m.MethodRef(
                m.TypeRefExpr("Tuple"),
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
            if (fn is CodeVariableReferenceExpression id)
            {
                var e = intrinsic.MaybeTranslate(id.Name, appl, args);
                if (e != null)
                    return e;
            }
            else
            {
                if (fn is CodeFieldReferenceExpression field)
                {
                    var specialTranslator = GetSpecialTranslator(field);
                    if (specialTranslator != null)
                    {
                        var special = specialTranslator(m, field, args);
                        if (special != null)
                            return special;
                    }

                    if (field.FieldName == "iteritems")
                    {
                        if (args.Length == 0)
                        {
                            // iteritems is Python 2.x returning an iterable over 
                            // a dictionary's key-value pairs. In C#, just return
                            // the dictionary (assumes that we're dealing with a dictionary!)
                        }
                        return field.Expression;
                    }
                    else if (field.FieldName == "itervalues")
                    {
                        if (args.Length == 0)
                        {
                            return m.Access(field.Expression, "Values");
                        }
                    }
                    else if (field.FieldName == "iterkeys")
                    {
                        if (args.Length == 0)
                        {
                            return m.Access(field.Expression, "Keys");
                        }
                    }
                }
            }
            return m.Appl(fn, args);
        }

        private Func<CodeGenerator, CodeFieldReferenceExpression, CodeExpression[], CodeExpression> GetSpecialTranslator(
            CodeFieldReferenceExpression field)
        {
            if (field.Expression is CodeVariableReferenceExpression id)
            {
                if (id.Name == "struct")
                {
                    return new Special.StructTranslator().Translate;
                }
            }
            return null;
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
                        return m.Prim(":");
                })
                .ToArray();
            return m.Aref(target, subs);
        }

        public CodeExpression VisitArgument(Argument a)
        {
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
                if (a.defval is CompFor compFor)
                {
                    var v = compFor.variable.Accept(this);
                    var c = Translate(v, compFor);
                    var mr = m.MethodRef(c, "Select");
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
                        a.defval?.Accept(this));
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

        public CodeExpression VisitAwait(AwaitExp awaitExp)
        {
            var exp = awaitExp.exp.Accept(this);
            return m.Await(exp);
        }

        public CodeExpression VisitRealLiteral(RealLiteral r)
        {
            if (r.Value == double.PositiveInfinity)
            {
                return m.Access(m.TypeRefExpr("double"), "PositiveInfinity");
            }
            else if (r.Value == double.NegativeInfinity)
            {
                return m.Access(m.TypeRefExpr("double"), "NegativeInfinity");
            }
            return m.Prim(r.Value);
        }


        public CodeExpression VisitDictInitializer(DictInitializer s)
        {
            var items = s.KeyValues.Select(kv => new CodeCollectionInitializer(
                kv.Key.Accept(this),
                kv.Value.Accept(this)));
            m.EnsureImport("System.Collections.Generic");
            var init = new CodeObjectCreateExpression
            {
                Type = m.TypeRef("Dictionary", "object", "object"),
                Initializer = new CodeCollectionInitializer
                {
                    Values = items.ToArray()
                }
            };
            return init;
        }

        public CodeExpression VisitSet(PySet s)
        {
            var items = s.exps.Select(e => new CodeCollectionInitializer(e.Accept(this)));
            m.EnsureImport("System.Collections");
            var init = new CodeObjectCreateExpression
            {
                Type = m.TypeRef("HashSet"),
                Initializer = new CodeCollectionInitializer
                {
                    Values = items.ToArray()
                }
            };
            return init;
        }

        public CodeExpression VisitSetComprehension(SetComprehension sc)
        {
            var compFor = (CompFor) sc.Collection;

            var v = compFor.variable.Accept(this);
            var c = Translate(v, compFor);

            if (IsIdentityProjection(compFor, sc.Projection))
            {
                return m.Appl(
                    m.MethodRef(
                        c,
                        "ToHashSet"));
            }
            else if (v is CodeVariableReferenceExpression)
            {
                return m.Appl(
                    m.MethodRef(
                        c,
                        "ToHashSet"),
                        m.Lambda(
                            new CodeExpression[] { v },
                            sc.Projection.Accept(this)));
            }
            else
            {
                var varList = (ExpList)compFor.variable;
                return
                    m.Appl(
                        m.MethodRef(
                            m.Appl(
                                m.MethodRef(
                                    c,
                                    "Chop"),
                                    m.Lambda(
                                         varList.Expressions.Select(e => e.Accept(this)).ToArray(),
                                         sc.Projection.Accept(this))),
                            "ToHashSet"));
            }
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
            if (u.op == Op.Sub && u.e is RealLiteral real)
            {
                return new RealLiteral(-real.Value, real.Filename, real.Start, real.End).Accept(this);
            }
            var e = u.e.Accept(this);
            return new CodeUnaryOperatorExpression(mppyoptocsop[u.op], e);
        }

        public CodeExpression VisitBigLiteral(BigLiteral bigLiteral)
        {
            return m.Prim(bigLiteral.Value);
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
            switch (bin.op)
            {
            case Op.Add:
                var cAdd = CondenseComplexConstant(l, r, 1);
                if (cAdd != null)
                    return cAdd;
                break;
            case Op.Sub:
                var cSub = CondenseComplexConstant(l, r, -1);
                if (cSub != null)
                    return cSub;
                break;
            }
            if (mppyoptocsop.TryGetValue(bin.op, out var opDst))
            {
                return m.BinOp(l, opDst, r);
            }
            switch (bin.op)
            {
            case Op.Is:
                if (bin.r is NoneExp)
                {
                    return m.BinOp(l, CodeOperatorType.IdentityEquality, r);
                }
                else
                {
                    return m.Appl(
                        m.MethodRef(
                            m.TypeRefExpr("object"),
                            "ReferenceEquals"),
                        l,
                        r);
                }
            case Op.IsNot:
                return m.BinOp(l, CodeOperatorType.IdentityInequality, r);
            case Op.In:
                return m.Appl(
                    m.MethodRef(r, "Contains"),
                     l);

            case Op.NotIn:
                return new CodeUnaryOperatorExpression(
                    CodeOperatorType.Not,
                    m.Appl(
                        m.MethodRef(r, "Contains"),
                        l));
            case Op.Exp:
                m.EnsureImport("System");
                return m.Appl(
                    m.MethodRef(m.TypeRefExpr("Math"), "Pow"),
                    l, r);
            }
            return m.BinOp(l, mppyoptocsop[bin.op], r);
        }

        private CodeExpression CondenseComplexConstant(CodeExpression l, CodeExpression r, double imScale)
        {
            if (r is CodePrimitiveExpression primR && primR.Value is Complex complexR &&
                l is CodePrimitiveExpression primL)
            {
                if (primL.Value is double doubleL)
                {
                    return m.Prim(new Complex(doubleL + complexR.Real, imScale * complexR.Imaginary));
                }
                else if (primL.Value is int intL)
                {
                    return m.Prim(new Complex(intL + complexR.Real, imScale * complexR.Imaginary));
                }
            }
            return null;
        }

        private CodeExpression TranslateFormatExpression(Exp formatString, Exp pyArgs)
        {
            var args = new List<CodeExpression>();
            args.Add(formatString.Accept(this));

            if (pyArgs is PyTuple tuple)
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
                m.MethodRef(
                    m.TypeRefExpr("String"),
                    "Format"),
                args.ToArray());
        }

        private bool IsIdentityProjection(CompFor compFor, Exp projection)
        {
            var idV = compFor.variable as Identifier;
            var idP = projection as Identifier;
            return (idV != null && idP != null && idV.Name == idP.Name);
        }

        public CodeExpression VisitListComprehension(ListComprehension lc)
        {
            var compFor = (CompFor) lc.Collection;
            var v = compFor.variable.Accept(this);
            var c = Translate(v, compFor);

            if (IsIdentityProjection(compFor, lc.Projection))
                return c;

            var mr = m.MethodRef(c, "Select");
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
                if (compFor.next is CompIf filter)
                {
                    return Where(c, v, filter.test.Accept(this));
                }
                if (compFor.next is CompFor join)
                {
                    //var pySrc = "((a, s) for a in stackframe.alocs.values() for s in a._segment_list)";
                    //string sExp = "stackframe.alocs.SelectMany(aa => aa._segment_list, (a, s) => Tuple.Create( a, s ))";
                    return m.Appl(
                        m.MethodRef(c, "SelectMany"),
                        m.Lambda(
                              new CodeExpression[] {((Identifier)compFor.variable).Accept(this) },
                            join.collection.Accept(this)),
                        m.Lambda(
                            new CodeExpression[] { 
                                ((Identifier)compFor.variable).Accept(this),
                                ((Identifier)join.variable).Accept(this)
                            },
                            m.Appl(
                                m.MethodRef(
                                    m.TypeRefExpr("Tuple"),
                                    "Create"),
                                ((Identifier)compFor.variable).Accept(this),
                                ((Identifier)join.variable).Accept(this))));
                }
            }
            return c;
        }

        private CodeExpression Where(CodeExpression c, CodeExpression v, CodeExpression filter)
        {
            var mr = m.MethodRef(c, "Where");
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
            return m.Access(exp, acc.FieldName.Name);
        }

        public CodeExpression VisitIdentifier(Identifier id)
        {
            if (id.Name == "self")
            {
                return new CodeThisReferenceExpression();
            }
            else
            {
                return gensym.MapLocalReference(id.Name);
            }
        }

        public CodeExpression VisitNoneExp()
        {
            return m.Prim(null);
        }

        public CodeExpression VisitBooleanLiteral(BooleanLiteral b)
        {
            return m.Prim(b.Value);
        }

        public CodeExpression VisitImaginary(ImaginaryLiteral im)
        {
            m.EnsureImport("System.Numerics");
            return m.Prim(new Complex(0, im.Value));
        }

        public CodeExpression VisitIntLiteral(IntLiteral i)
        {
            return m.Prim(i.Value);
        }

        public CodeExpression VisitLongLiteral(LongLiteral l)
        {
            return m.Prim(l.Value);
        }

        public CodeExpression VisitStarExp(StarExp s)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitBytes(Bytes b)
        {
            return m.Prim(b);
        }

        public CodeExpression VisitStr(Str s)
        {
            return m.Prim(s);
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
            var fn = m.MethodRef(
                m.TypeRefExpr("Tuple"), "Create");
            if (tuple.values.Count == 0)
            {
                return m.Appl(fn, m.Prim("<Empty>"));
            }
            else
            {
                return m.Appl(fn, tuple.values.Select(v => v.Accept(this)).ToArray());
            }
        }

        public CodeExpression VisitYieldExp(YieldExp yieldExp)
        {
            if (yieldExp.exp == null)
                return m.Prim(null);
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
            switch (dc.source.variable)
            {
            case ExpList varList:
                {
                    //if (varList.Expressions.Count != 2)
                    //    throw new InvalidOperationException("Variable list should contain one or two variables.");
                    var args = varList.Expressions.Select((e, i) => Tuple.Create(e, string.Format($"Item{i + 1}")))
                        .ToDictionary(d => d.Item1, d => d.Item2);
                    var tpl = gensym.GenSymAutomatic("_tup_", null, false);

                    gensym.PushIdMappings(varList.Expressions.ToDictionary(e => e.ToString(), e => m.Access(tpl, args[e])));

                    var kValue = dc.key.Accept(this);
                    var vValue = dc.value.Accept(this);

                    gensym.PopIdMappings();

                    return m.Appl(
                        m.MethodRef(list, "ToDictionary"),
                        m.Lambda(new CodeExpression[] { tpl }, kValue),
                        m.Lambda(new CodeExpression[] { tpl }, vValue));
                }
            case Identifier id:
                {
                    var kValue = dc.key.Accept(this);
                    var vValue = dc.value.Accept(this);
                    return m.Appl(
                        m.MethodRef(list, "ToDictionary"),
                        m.Lambda(new CodeExpression[] { id.Accept(this) }, kValue),
                        m.Lambda(new CodeExpression[] { id.Accept(this) }, vValue));
                }
            case PyTuple tuple:
                {
                    if (tuple.values.Count != 2)
                    {
                        //TODO: tuples, especially nested tuples, are hard.
                        return m.Prim("!!!{" +
                            dc.key.Accept(this) +
                            ": " +
                            dc.value.Accept(this));
                    }

                    var enumvar = gensym.GenSymAutomatic("_de", null, false);
                    gensym.PushIdMappings(new Dictionary<string, CodeExpression>
                {
                    { tuple.values[0].ToString(), m.Access(enumvar, "Key") },
                    { tuple.values[1].ToString(), m.Access(enumvar, "Value") },
                });

                    var kValue = dc.key.Accept(this);
                    var vValue = dc.value.Accept(this);

                    gensym.PopIdMappings();

                    return m.Appl(
                        m.MethodRef(list, "ToDictionary"),
                        m.Lambda(new CodeExpression[] { enumvar }, kValue),
                        m.Lambda(new CodeExpression[] { enumvar }, vValue));
                }
            }
            throw new NotImplementedException();
        }
    }
}