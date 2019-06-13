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

using Pytocs.Core.Syntax;
using Pytocs.Core.CodeModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Pytocs.Core.Types;

namespace Pytocs.Core.Translate
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
            { Op.AugIDiv, CodeOperatorType.DivEq },
            { Op.AugAnd, CodeOperatorType.AndEq },
            { Op.AugOr, CodeOperatorType.OrEq },
            { Op.AugMod, CodeOperatorType.ModEq },
            { Op.AugShl, CodeOperatorType.ShlEq },
            { Op.AugShr, CodeOperatorType.ShrEq },
            { Op.AugXor, CodeOperatorType.XorEq },
        };

        internal CodeGenerator m;
        internal SymbolGenerator gensym;
        internal IntrinsicTranslator intrinsic;
        private TypeReferenceTranslator types;

        public ExpTranslator(TypeReferenceTranslator types, CodeGenerator gen, SymbolGenerator gensym)
        {
            this.types = types;
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
            var fn = appl.fn.Accept(this);
            var args = TranslateArgs(appl).ToArray();
            var dtFn = types.TypeOf(appl.fn);
            if (dtFn is ClassType c)
            {
                // applying a class type => calling constructor.
                var (csClass, nm) = types.Translate(dtFn);
                m.EnsureImports(nm);
                var e = m.New(csClass, args);
                return e;
            }
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
                    var v = a.name.Accept(this);
                    var c = TranslateToLinq(v, compFor);
                    return c;
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
            if (s.KeyValues.All(kv => kv.Key != null))
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
            else
            {
                m.EnsureImport("pytocs.runtime");
                // There was a dictionary unpacking present. 
                var items = s.KeyValues.Select(kv =>
                {
                    var v = kv.Value.Accept(this);
                    if (kv.Key != null)
                    {
                        var k = kv.Key.Accept(this);
                        return m.ValueTuple(k, v);
                    }
                    else
                    {
                        return v;
                    }
                });
                var unpack = m.MethodRef(m.TypeRefExpr("DictionaryUtils"),"Unpack");
                unpack.TypeReferences.Add(m.TypeRef(typeof(string)));
                unpack.TypeReferences.Add(m.TypeRef(typeof(object)));
                return m.Appl(unpack, items.ToArray());
            }
        }

        public CodeExpression VisitSet(PySet s)
        {
            m.EnsureImport("System.Collections");
            var items = s.exps.Select(e => new CodeCollectionInitializer(e.Accept(this)));
            var init = m.New(m.TypeRef("HashSet"), items.ToArray());
            return init;
        }

        public CodeExpression VisitSetComprehension(SetComprehension sc)
        {
            m.EnsureImport("System.Linq");
            var compFor = (CompFor) sc.Collection;
            var v = sc.Projection.Accept(this);
            var c = TranslateToLinq(v, compFor);
            return m.Appl(
                m.MethodRef(
                    c,
                    "ToHashSet"));
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
            var arg = formatString.Accept(this);
            var args = new List<CodeExpression> { arg };
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
            m.EnsureImport("System.Linq");
            var compFor = (CompFor) lc.Collection;
            var v = lc.Projection.Accept(this);
            var c = TranslateToLinq(v, compFor);
            return m.Appl(
                m.MethodRef(
                    c,
                    "ToList"));
        }

        public CodeExpression VisitLambda(Lambda l)
        {
            var e = new CodeLambdaExpression(
                l.args.Select(a => a.name.Accept(this)).ToArray(),
                l.body.Accept(this));
            return e;
        }

        private CodeExpression TranslateToLinq(CodeExpression projection, CompFor compFor)
        {
            var e = compFor.collection.Accept(this);
            var queryClauses = new List<CodeQueryClause>();
            From(compFor.variable, e, queryClauses);
            var iter = compFor.next;
            while (iter != null)
            {
                if (iter is CompIf filter)
                {
                    e = filter.test.Accept(this);
                    queryClauses.Add(m.Where(e));
                }
                else if (iter is CompFor join)
                {
                    e = join.collection.Accept(this);
                    From(join.variable, e, queryClauses);
                }
                iter = iter.next;
            }
            queryClauses.Add(m.Select(projection));
            return m.Query(queryClauses.ToArray());
        }


        private void From(Exp variable, CodeExpression collection, List<CodeQueryClause> queryClauses)
        {
            switch (variable)
            {
            case Identifier id:
            {
                var f = m.From(id.Accept(this), collection);
                queryClauses.Add(f);
                break;
            }
            case ExpList expList:
            {
                var vars = expList.Expressions.Select(v => v.Accept(this)).ToArray();
                var type = MakeTupleType(expList.Expressions);
                var it = gensym.GenSymAutomatic("_tup_", type, false);
                var f = m.From(it, m.ApplyMethod(collection, "Chop",
                    m.Lambda(vars,
                    MakeTupleCreate(vars))));
                queryClauses.Add(f);
                for (int i = 0; i < vars.Length; ++i)
                {
                    var l = m.Let(vars[i], m.Access(it, $"Item{i + 1}"));
                    queryClauses.Add(l);
                }
                break;
            }
            case PyTuple tuple:
            {
                var vars = tuple.values.Select(v => v.Accept(this)).ToArray();
                var type = MakeTupleType(tuple.values);
                var it = gensym.GenSymAutomatic("_tup_", type, false);
                var f = m.From(it, m.ApplyMethod(collection, "Chop",
                    m.Lambda(vars,
                    MakeTupleCreate(vars))));
                queryClauses.Add(f);
                for (int i = 0; i < vars.Length; ++i)
                {
                    var l = m.Let(vars[i], m.Access(it, $"Item{i + 1}"));
                    queryClauses.Add(l);
                }
                break;
            }
            default:
                throw new NotImplementedException(variable.GetType().Name);
            }
        }

        private CodeTypeReference MakeTupleType(List<Exp> expressions)
        {
            //$TODO: make an effor to make this correct.
            return new CodeTypeReference(typeof(object));
        }

        public CodeExpression VisitList(PyList l)
        {
            var (elemType, nms) = types.TranslateListElementType(l);
            m.EnsureImports(nms);
            return m.ListInitializer(
                elemType,
                l.elts
                .Where(e => e != null)
                .Select(e => e.Accept(this)));
        }

        public CodeExpression VisitFieldAccess(AttributeAccess acc)
        {
            var exp = acc.Expression.Accept(this);
            return m.Access(exp, acc.FieldName.Name);
        }

        public CodeExpression VisitGeneratorExp(GeneratorExp g)
        {
            m.EnsureImport("System.Linq");
            var compFor = (CompFor)g.Collection;
            var v = g.Projection.Accept(this);
            var c = TranslateToLinq(v, compFor);
            return c;
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
            if (tuple.values.Count == 0)
            {
                return MakeTupleCreate(m.Prim("<Empty>"));
            }
            else
            {
                return MakeTupleCreate(tuple.values.Select(v => v.Accept(this)).ToArray());
            }
        }

        private CodeExpression MakeTupleCreate(params CodeExpression[] exprs)
        {
            var fn = m.MethodRef(
                m.TypeRefExpr("Tuple"), "Create");
            return m.Appl(fn, exprs);
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