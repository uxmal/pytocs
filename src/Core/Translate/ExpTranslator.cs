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
using Pytocs.Core.Translate.Special;
using Pytocs.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Pytocs.Core.Translate
{
    /// <summary>
    ///     Translates a Python expression.
    /// </summary>
    public class ExpTranslator : IExpVisitor<CodeExpression>
    {
        private static readonly Dictionary<Op, CodeOperatorType> mppyoptocsop = new Dictionary<Op, CodeOperatorType>
        {
            {Op.Add, CodeOperatorType.Add},
            {Op.Sub, CodeOperatorType.Sub},
            {Op.Eq, CodeOperatorType.Equal},
            {Op.Ne, CodeOperatorType.NotEqual},
            {Op.Gt, CodeOperatorType.Gt},
            {Op.Ge, CodeOperatorType.Ge},
            {Op.Le, CodeOperatorType.Le},
            {Op.Lt, CodeOperatorType.Lt},
            {Op.LogAnd, CodeOperatorType.LogAnd},
            {Op.BitAnd, CodeOperatorType.BitAnd},
            {Op.LogOr, CodeOperatorType.LogOr},
            {Op.BitOr, CodeOperatorType.BitOr},
            {Op.Not, CodeOperatorType.Not},
            {Op.Mod, CodeOperatorType.Mod},
            {Op.Div, CodeOperatorType.Div},
            {Op.Mul, CodeOperatorType.Mul},
            {Op.Shr, CodeOperatorType.Shr},
            {Op.Shl, CodeOperatorType.Shl},
            {Op.Complement, CodeOperatorType.Complement},
            {Op.IDiv, CodeOperatorType.Div},
            {Op.Xor, CodeOperatorType.BitXor},
            {Op.AugAdd, CodeOperatorType.AddEq},
            {Op.AugSub, CodeOperatorType.SubEq},
            {Op.AugMul, CodeOperatorType.MulEq},
            {Op.AugDiv, CodeOperatorType.DivEq},
            {Op.AugIDiv, CodeOperatorType.DivEq},
            {Op.AugAnd, CodeOperatorType.AndEq},
            {Op.AugOr, CodeOperatorType.OrEq},
            {Op.AugMod, CodeOperatorType.ModEq},
            {Op.AugShl, CodeOperatorType.ShlEq},
            {Op.AugShr, CodeOperatorType.ShrEq},
            {Op.AugXor, CodeOperatorType.XorEq}
        };

        internal readonly ClassDef classDef;
        internal SymbolGenerator gensym;
        internal IntrinsicTranslator intrinsic;
        internal CodeGenerator m;
        private readonly TypeReferenceTranslator types;

        public ExpTranslator(ClassDef classDef, TypeReferenceTranslator types, CodeGenerator gen,
            SymbolGenerator gensym)
        {
            this.classDef = classDef;
            this.types = types;
            m = gen;
            this.gensym = gensym;
            intrinsic = new IntrinsicTranslator(this);
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
            CodeMethodReferenceExpression fn = m.MethodRef(
                m.TypeRefExpr("Tuple"),
                "Create");
            return m.Appl
            (fn,
                l.Expressions.Select(e => e.Accept(this)).ToArray());
        }

        public CodeExpression VisitApplication(Application appl)
        {
            CodeExpression fn = appl.fn.Accept(this);
            CodeExpression[] args = TranslateArgs(appl).ToArray();
            DataType dtFn = types.TypeOf(appl.fn);
            if (dtFn is ClassType c)
            {
                // applying a class type => calling constructor.
                (CodeTypeReference csClass, ISet<string> nm) = types.Translate(dtFn);
                m.EnsureImports(nm);
                CodeObjectCreateExpression e = m.New(csClass, args);
                return e;
            }

            if (fn is CodeVariableReferenceExpression id)
            {
                CodeExpression e = intrinsic.MaybeTranslate(id.Name, appl, args);
                if (e != null)
                {
                    return e;
                }
            }
            else
            {
                if (fn is CodeFieldReferenceExpression field)
                {
                    Func<CodeGenerator, CodeFieldReferenceExpression, CodeExpression[], CodeExpression>
                        specialTranslator = GetSpecialTranslator(field);
                    if (specialTranslator != null)
                    {
                        CodeExpression special = specialTranslator(m, field, args);
                        if (special != null)
                        {
                            return special;
                        }
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

                    if (field.FieldName == "itervalues")
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

        public CodeExpression VisitArrayRef(ArrayRef aref)
        {
            CodeExpression target = aref.array.Accept(this);
            CodeExpression[] subs = aref.subs
                .Select(s =>
                {
                    if (s.upper != null || s.step != null)
                    {
                        return new CodeVariableReferenceExpression(string.Format(
                            "{0}:{1}:{2}",
                            s.lower, s.upper, s.step));
                    }

                    if (s.lower != null)
                    {
                        return s.lower.Accept(this);
                    }

                    if (s.upper != null)
                    {
                        return s.upper.Accept(this);
                    }

                    if (s.step != null)
                    {
                        return s.step.Accept(this);
                    }

                    return m.Prim(":");
                })
                .ToArray();
            return m.Aref(target, subs);
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
            CodeExpression exp = awaitExp.exp.Accept(this);
            return m.Await(exp);
        }

        public CodeExpression VisitRealLiteral(RealLiteral r)
        {
            if (r.NumericValue == double.PositiveInfinity)
            {
                return m.Access(m.TypeRefExpr("double"), "PositiveInfinity");
            }

            if (r.NumericValue == double.NegativeInfinity)
            {
                return m.Access(m.TypeRefExpr("double"), "NegativeInfinity");
            }

            return m.Prim(r.NumericValue);
        }

        public CodeExpression VisitDictInitializer(DictInitializer s)
        {
            if (s.KeyValues.All(kv => kv.Key != null))
            {
                IEnumerable<CodeCollectionInitializer> items = s.KeyValues.Select(kv => new CodeCollectionInitializer(
                    kv.Key.Accept(this),
                    kv.Value.Accept(this)));
                m.EnsureImport("System.Collections.Generic");
                CodeObjectCreateExpression init = new CodeObjectCreateExpression
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
                IEnumerable<CodeExpression> items = s.KeyValues.Select(kv =>
                {
                    CodeExpression v = kv.Value.Accept(this);
                    if (kv.Key != null)
                    {
                        CodeExpression k = kv.Key.Accept(this);
                        return m.ValueTuple(k, v);
                    }

                    return v;
                });
                CodeMethodReferenceExpression unpack = m.MethodRef(m.TypeRefExpr("DictionaryUtils"), "Unpack");
                unpack.TypeReferences.Add(m.TypeRef(typeof(string)));
                unpack.TypeReferences.Add(m.TypeRef(typeof(object)));
                return m.Appl(unpack, items.ToArray());
            }
        }

        public CodeExpression VisitSet(PySet s)
        {
            m.EnsureImport("System.Collections");
            if (s.exps.OfType<IterableUnpacker>().Any())
            {
                // Found iterable unpackers.
                List<CodeExpression> seq = new List<CodeExpression>();
                List<CodeExpression> subseq = new List<CodeExpression>();
                foreach (Exp item in s.exps)
                {
                    CodeExpression exp = null;
                    if (item is IterableUnpacker unpacker)
                    {
                        if (subseq.Count > 0)
                        {
                            exp = m.NewArray(m.TypeRef(typeof(object)), subseq.ToArray());
                            seq.Add(exp);
                            subseq.Clear();
                        }

                        exp = unpacker.Iterable.Accept(this);
                        seq.Add(exp);
                    }
                    else
                    {
                        exp = item.Accept(this);
                        subseq.Add(exp);
                    }
                }

                if (subseq.Count > 0)
                {
                    CodeArrayCreateExpression exp = m.NewArray(m.TypeRef(typeof(object)), subseq.ToArray());
                    seq.Add(exp);
                }

                CodeMethodReferenceExpression unpack = m.MethodRef(m.TypeRefExpr("SetUtils"), "Unpack");
                unpack.TypeReferences.Add(m.TypeRef(typeof(object)));
                return m.Appl(unpack, seq.ToArray());
            }

            IEnumerable<CodeCollectionInitializer> items =
                s.exps.Select(e => new CodeCollectionInitializer(e.Accept(this)));
            CodeObjectCreateExpression init = m.New(m.TypeRef("HashSet"), items.ToArray());
            return init;
        }

        public CodeExpression VisitSetComprehension(SetComprehension sc)
        {
            m.EnsureImport("System.Linq");
            CompFor compFor = (CompFor)sc.Collection;
            CodeExpression v = sc.Projection.Accept(this);
            CodeExpression c = TranslateToLinq(v, compFor);
            return m.Appl(
                m.MethodRef(
                    c,
                    "ToHashSet"));
        }

        public CodeExpression VisitUnary(UnaryExp u)
        {
            if (u.op == Op.Sub && u.e is RealLiteral real)
            {
                return new RealLiteral("-" + real.Value, -real.NumericValue, real.Filename, real.Start, real.End)
                    .Accept(this);
            }

            CodeExpression e = u.e.Accept(this);
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

            CodeExpression l = bin.l.Accept(this);
            CodeExpression r = bin.r.Accept(this);
            switch (bin.op)
            {
                case Op.Add:
                    CodeExpression cAdd = CondenseComplexConstant(l, r, 1);
                    if (cAdd != null)
                    {
                        return cAdd;
                    }

                    break;

                case Op.Sub:
                    CodeExpression cSub = CondenseComplexConstant(l, r, -1);
                    if (cSub != null)
                    {
                        return cSub;
                    }

                    break;
            }

            if (mppyoptocsop.TryGetValue(bin.op, out CodeOperatorType opDst))
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

        public CodeExpression VisitListComprehension(ListComprehension lc)
        {
            m.EnsureImport("System.Linq");
            CompFor compFor = (CompFor)lc.Collection;
            CodeExpression v = lc.Projection.Accept(this);
            CodeExpression c = TranslateToLinq(v, compFor);
            return m.Appl(
                m.MethodRef(
                    c,
                    "ToList"));
        }

        public CodeExpression VisitIterableUnpacker(IterableUnpacker unpacker)
        {
            throw new NotImplementedException();
        }

        public CodeExpression VisitLambda(Lambda l)
        {
            CodeLambdaExpression e = new CodeLambdaExpression(
                l.args.Select(a => a.name.Accept(this)).ToArray(),
                l.body.Accept(this));
            return e;
        }

        public CodeExpression VisitList(PyList l)
        {
            (CodeTypeReference elemType, ISet<string> nms) = types.TranslateListElementType(l);
            m.EnsureImports(nms);

            if (l.elts.OfType<StarExp>().Any())
            {
                // Found iterable unpackers.
                List<CodeExpression> seq = new List<CodeExpression>();
                List<CodeExpression> subseq = new List<CodeExpression>();
                foreach (Exp item in l.elts)
                {
                    CodeExpression exp = null;
                    if (item is StarExp unpacker)
                    {
                        if (subseq.Count > 0)
                        {
                            exp = m.NewArray(elemType, subseq.ToArray());
                            seq.Add(exp);
                            subseq.Clear();
                        }

                        exp = unpacker.e.Accept(this);
                        seq.Add(exp);
                    }
                    else
                    {
                        exp = item.Accept(this);
                        subseq.Add(exp);
                    }
                }

                if (subseq.Count > 0)
                {
                    CodeArrayCreateExpression exp = m.NewArray(elemType, subseq.ToArray());
                    seq.Add(exp);
                }

                CodeMethodReferenceExpression unpack = m.MethodRef(m.TypeRefExpr("ListUtils"), "Unpack");
                unpack.TypeReferences.Add(elemType);
                return m.Appl(unpack, seq.ToArray());
            }

            return m.ListInitializer(
                elemType,
                l.elts
                    .Where(e => e != null)
                    .Select(e => e.Accept(this)));
        }

        public CodeExpression VisitFieldAccess(AttributeAccess acc)
        {
            CodeExpression exp = acc.Expression.Accept(this);
            return m.Access(exp, acc.FieldName.Name);
        }

        public CodeExpression VisitGeneratorExp(GeneratorExp g)
        {
            m.EnsureImport("System.Linq");
            CompFor compFor = (CompFor)g.Collection;
            CodeExpression v = g.Projection.Accept(this);
            CodeExpression c = TranslateToLinq(v, compFor);
            return c;
        }

        public CodeExpression VisitIdentifier(Identifier id)
        {
            if (id.Name == "self")
            {
                return m.This();
            }

            return gensym.MapLocalReference(id.Name);
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
            return m.Prim(new Complex(0, im.NumericValue));
        }

        public CodeExpression VisitIntLiteral(IntLiteral i)
        {
            return m.Number(i.Value);
        }

        public CodeExpression VisitLongLiteral(LongLiteral l)
        {
            return m.Number(l.Value);
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
            CodeExpression cond = t.Condition.Accept(this);
            CodeExpression cons = t.Consequent.Accept(this);
            CodeExpression alt = t.Alternative.Accept(this);
            return new CodeConditionExpression(cond, cons, alt);
        }

        public CodeExpression VisitTuple(PyTuple tuple)
        {
            if (tuple.values.Count == 0)
            {
                return MakeTupleCreate(m.Prim("<Empty>"));
            }

            return MakeTupleCreate(tuple.values.Select(v => v.Accept(this)).ToArray());
        }

        public CodeExpression VisitYieldExp(YieldExp yieldExp)
        {
            if (yieldExp.exp == null)
            {
                return m.Prim(null);
            }

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

            CodeExpression list = dc.source.collection.Accept(this);
            switch (dc.source.variable)
            {
                case ExpList varList:
                    {
                        //if (varList.Expressions.Count != 2)
                        //    throw new InvalidOperationException("Variable list should contain one or two variables.");
                        Dictionary<Exp, string> args = varList.Expressions
                            .Select((e, i) => Tuple.Create(e, string.Format($"Item{i + 1}")))
                            .ToDictionary(d => d.Item1, d => d.Item2);
                        CodeVariableReferenceExpression tpl = gensym.GenSymAutomatic("_tup_", null, false);

                        gensym.PushIdMappings(
                            varList.Expressions.ToDictionary(e => e.ToString(), e => m.Access(tpl, args[e])));

                        CodeExpression kValue = dc.key.Accept(this);
                        CodeExpression vValue = dc.value.Accept(this);

                        gensym.PopIdMappings();

                        return m.Appl(
                            m.MethodRef(list, "ToDictionary"),
                            m.Lambda(new CodeExpression[] { tpl }, kValue),
                            m.Lambda(new CodeExpression[] { tpl }, vValue));
                    }
                case Identifier id:
                    {
                        CodeExpression kValue = dc.key.Accept(this);
                        CodeExpression vValue = dc.value.Accept(this);
                        return m.Appl(
                            m.MethodRef(list, "ToDictionary"),
                            m.Lambda(new[] { id.Accept(this) }, kValue),
                            m.Lambda(new[] { id.Accept(this) }, vValue));
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

                        CodeVariableReferenceExpression enumvar = gensym.GenSymAutomatic("_de", null, false);
                        gensym.PushIdMappings(new Dictionary<string, CodeExpression>
                    {
                        {tuple.values[0].ToString(), m.Access(enumvar, "Key")},
                        {tuple.values[1].ToString(), m.Access(enumvar, "Value")}
                    });

                        CodeExpression kValue = dc.key.Accept(this);
                        CodeExpression vValue = dc.value.Accept(this);

                        gensym.PopIdMappings();

                        return m.Appl(
                            m.MethodRef(list, "ToDictionary"),
                            m.Lambda(new CodeExpression[] { enumvar }, kValue),
                            m.Lambda(new CodeExpression[] { enumvar }, vValue));
                    }
            }

            throw new NotImplementedException();
        }

        private IEnumerable<CodeExpression> TranslateArgs(Application args)
        {
            foreach (Argument arg in args.args)
            {
                yield return VisitArgument(arg);
            }

            foreach (Argument arg in args.keywords)
            {
                yield return VisitArgument(arg);
            }

            if (args.stargs != null)
            {
                yield return args.stargs.Accept(this);
            }

            if (args.kwargs != null)
            {
                yield return args.kwargs.Accept(this);
            }
        }

        private Func<CodeGenerator, CodeFieldReferenceExpression, CodeExpression[], CodeExpression>
            GetSpecialTranslator(
                CodeFieldReferenceExpression field)
        {
            if (field.Expression is CodeVariableReferenceExpression id)
            {
                if (id.Name == "struct")
                {
                    return new StructTranslator().Translate;
                }
            }

            return null;
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

            if (a.defval is CompFor compFor)
            {
                CodeExpression v = a.name.Accept(this);
                CodeExpression c = TranslateToLinq(v, compFor);
                return c;
            }

            return new CodeNamedArgument(
                a.name.Accept(this),
                a.defval?.Accept(this));
        }

        private CodeExpression CondenseComplexConstant(CodeExpression l, CodeExpression r, double imScale)
        {
            if (r is CodePrimitiveExpression primR && primR.Value is Complex complexR)
            {
                if (l is CodePrimitiveExpression primL)
                {
                    if (primL.Value is double doubleL)
                    {
                        return m.Prim(new Complex(doubleL + complexR.Real, imScale * complexR.Imaginary));
                    }

                    if (primL.Value is int intL)
                    {
                        return m.Prim(new Complex(intL + complexR.Real, imScale * complexR.Imaginary));
                    }
                }
                else if (l is CodeNumericLiteral litL)
                {
                    long intL = Convert.ToInt64(litL.Literal, CultureInfo.InvariantCulture);
                    return m.Prim(new Complex(intL + complexR.Real, imScale * complexR.Imaginary));
                }
            }

            return null;
        }

        private CodeExpression TranslateFormatExpression(Exp formatString, Exp pyArgs)
        {
            CodeExpression arg = formatString.Accept(this);
            List<CodeExpression> args = new List<CodeExpression> { arg };
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
            Identifier idV = compFor.variable as Identifier;
            Identifier idP = projection as Identifier;
            return idV != null && idP != null && idV.Name == idP.Name;
        }

        private CodeExpression TranslateToLinq(CodeExpression projection, CompFor compFor)
        {
            CodeExpression e = compFor.collection.Accept(this);
            List<CodeQueryClause> queryClauses = new List<CodeQueryClause>();
            From(compFor.variable, e, queryClauses);
            CompIter iter = compFor.next;
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
                        CodeFromClause f = m.From(id.Accept(this), collection);
                        queryClauses.Add(f);
                        break;
                    }
                case ExpList expList:
                    {
                        CodeExpression[] vars = expList.Expressions.Select(v => v.Accept(this)).ToArray();
                        CodeTypeReference type = MakeTupleType(expList.Expressions);
                        CodeVariableReferenceExpression it = gensym.GenSymAutomatic("_tup_", type, false);
                        CodeFromClause f = m.From(it, m.ApplyMethod(collection, "Chop",
                            m.Lambda(vars,
                                MakeTupleCreate(vars))));
                        queryClauses.Add(f);
                        for (int i = 0; i < vars.Length; ++i)
                        {
                            CodeLetClause l = m.Let(vars[i], m.Access(it, $"Item{i + 1}"));
                            queryClauses.Add(l);
                        }

                        break;
                    }
                case PyTuple tuple:
                    {
                        CodeExpression[] vars = tuple.values.Select(v => v.Accept(this)).ToArray();
                        CodeTypeReference type = MakeTupleType(tuple.values);
                        CodeVariableReferenceExpression it = gensym.GenSymAutomatic("_tup_", type, false);
                        CodeFromClause f = m.From(it, m.ApplyMethod(collection, "Chop",
                            m.Lambda(vars,
                                MakeTupleCreate(vars))));
                        queryClauses.Add(f);
                        for (int i = 0; i < vars.Length; ++i)
                        {
                            CodeLetClause l = m.Let(vars[i], m.Access(it, $"Item{i + 1}"));
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
            //$TODO: make an effort to make this correct.
            return new CodeTypeReference(typeof(object));
        }

        private CodeExpression MakeTupleCreate(params CodeExpression[] exprs)
        {
            return m.ValueTuple(exprs);
        }
    }
}