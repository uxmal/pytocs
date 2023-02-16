#region License
//  Copyright 2015-2021 John Källén
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
using System.Numerics;
using Pytocs.Core.Types;
using System.Globalization;

namespace Pytocs.Core.Translate
{
    /// <summary>
    /// Translates a Python expression.
    /// </summary>
    public class ExpTranslator : IExpVisitor<CodeExpression>
    {
        private static readonly Dictionary<Op, CodeOperatorType> mppyoptocsop = new Dictionary<Op, CodeOperatorType>() 
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
            { Op.Assign, CodeOperatorType.Assign },
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

        private static readonly HashSet<CodeOperatorType> comparisons = new HashSet<CodeOperatorType>
        {
            CodeOperatorType.Equal,
            CodeOperatorType.NotEqual,
            CodeOperatorType.Gt,
            CodeOperatorType.Ge,
            CodeOperatorType.Le,
            CodeOperatorType.Lt,
        };

        internal readonly ClassDef? classDef;
        internal readonly CodeGenerator m;
        internal readonly SymbolGenerator gensym;
        internal readonly IntrinsicTranslator intrinsic;
        private readonly TypeReferenceTranslator types;

        public ExpTranslator(ClassDef? classDef, TypeReferenceTranslator types, CodeGenerator gen, SymbolGenerator gensym)
        {
            this.classDef = classDef;
            this.types = types;
            this.m = gen;
            this.gensym = gensym;
            this.intrinsic = new IntrinsicTranslator(this);
        }

        public CodeExpression VisitCompFor(CompFor compFor)
        {
            var p = compFor.projection?.Accept(this);
            var cf = TranslateToLinq(p, compFor);
            return cf;
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
            return new CodeDefaultExpression();
        }

        public CodeExpression VisitExpList(ExpList l)
        {
            //$TODO: there is no notion of a list element type.
            var (elemType, nms) = types.TranslateListElementType(l);
            if (ContainsStarExp(l.Expressions))
            {
                return ExpandIterableExpanders("TupleUtils", l.Expressions, elemType);
        }
            else
            {
                return m.ValueTuple(l.Expressions.Select(e => e.Accept(this)));
            }
        }

        private IEnumerable<CodeExpression> TranslateArgs(Application args)
        {
            foreach (var arg in args.Args)
            {
                yield return VisitArgument(arg);
            }
            foreach (var arg in args.Keywords)
            {
                yield return VisitArgument(arg);
            }
            if (args.StArgs != null)
                yield return args.StArgs.Accept(this);
            if (args.KwArgs != null)
                yield return args.KwArgs.Accept(this);
        }

        public CodeExpression VisitApplication(Application appl)
        {
            var fn = appl.Function.Accept(this);
            var args = TranslateArgs(appl).ToArray();
            var dtFn = types.TypeOf(appl.Function);
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

        private Func<CodeGenerator, CodeFieldReferenceExpression, CodeExpression[], CodeExpression?>? GetSpecialTranslator(
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
            var target = aref.Array.Accept(this);
            var subs = aref.Subs
                .Select(s =>
                {
                    if (s.Stride != null || s.Upper != null)
                        return new CodeVariableReferenceExpression(string.Format(
                            "{0}:{1}:{2}",
                            s.Lower, s.Upper, s.Stride));
                    if (s.Lower != null)
                    {
                        var e = s.Lower.Accept(this);
                        if (e is CodeUnaryOperatorExpression u &&
                            u.Operator == CodeOperatorType.Sub && 
                            u.Expression is CodeNumericLiteral)
                        {
                            return new CodeUnaryOperatorExpression(CodeOperatorType.Index, u.Expression);
                        }
                        return e;
                    }
                    else if (s.Upper != null)
                        return s.Upper.Accept(this);
                    else if (s.Stride != null)
                        return s.Stride.Accept(this);
                    else
                        return m.Prim(":");
                })
                .ToArray();
            return m.Aref(target, subs);
        }

        public CodeExpression VisitArgument(Argument a)
        {
            var argType = types.TranslateTypeOf(a);
            if (a is ListArgument)
            {
                return m.Appl(
                    new CodeVariableReferenceExpression("__flatten___"),
                    new CodeNamedArgument(a.Name!.Accept(this), null));
            }
            if (a.Name == null)
            {
                Debug.Assert(a.DefaultValue != null);
                return a.DefaultValue.Accept(this);
            }
            else
            {
                if (a.DefaultValue is CompFor compFor)
                {
                    var v = a.Name.Accept(this);
                    var c = TranslateToLinq(v, compFor);
                    return c;
                }
                else
                {
                    return new CodeNamedArgument(
                        a.Name.Accept(this),
                        a.DefaultValue?.Accept(this));
                }
            }
        }

        public CodeExpression VisitAssignExp(AssignExp e)
        {
            var d = e.Dst.Accept(this);
            var s = e.Src.Accept(this);
            if (e.Operator == Op.AugMatMul)
            {
                return m.AssignExp(d, 
                    m.ApplyMethod(d, "__imatmul__", s));
            }
            return m.BinOp(d, mppyoptocsop[e.Operator], s);
        }

        public CodeExpression VisitAssignmentExp(AssignmentExp e)
        {
            var id = gensym.MapLocalReference(e.Dst.Name);
            var exp = e.Src.Accept(this);
            return m.BinOp(
                    m.AssignExp(id, exp),
                    CodeOperatorType.NotEqual,
                    m.Prim(null));
        }

        public CodeExpression VisitAwait(AwaitExp awaitExp)
        {
            var exp = awaitExp.Exp.Accept(this);
            return m.Await(exp);
        }

        public CodeExpression VisitRealLiteral(RealLiteral r)
        {
            if (r.NumericValue == double.PositiveInfinity)
            {
                return m.Access(m.TypeRefExpr("double"), "PositiveInfinity");
        }
            else if (r.NumericValue == double.NegativeInfinity)
            {
                return m.Access(m.TypeRefExpr("double"), "NegativeInfinity");
            }
            return m.Prim(r.NumericValue);
        }


        public CodeExpression VisitDictInitializer(DictInitializer s)
        {
            if (s.KeyValues.All(kv => kv.Key != null))
            {
            var items = s.KeyValues.Select(kv => new CodeCollectionInitializer(
                    kv.Key!.Accept(this),
                kv.Value.Accept(this)));
                m.EnsureImport(TypeReferenceTranslator.GenericCollectionNamespace);
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
            if (s.Initializer.OfType<IterableUnpacker>().Any())
            {
                // Found iterable unpackers.
                var seq = new List<CodeExpression>();
                var subseq = new List<CodeExpression>();
                foreach (var item in s.Initializer)
                {
                    CodeExpression? exp = null;
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
                    var exp = m.NewArray(m.TypeRef(typeof(object)), subseq.ToArray());
                    seq.Add(exp);
                }

                var unpack = m.MethodRef(m.TypeRefExpr("SetUtils"), "Unpack");
                unpack.TypeReferences.Add(m.TypeRef(typeof(object)));
                return m.Appl(unpack, seq.ToArray());

            }
            else
            {
                var items = s.Initializer.Select(e => e.Accept(this));
                var init = m.New(m.TypeRef("HashSet"));
                init.Initializers.AddRange(items);
                return init;
            }
        }

        public CodeExpression VisitSetComprehension(SetComprehension sc)
            {
            m.EnsureImport(TypeReferenceTranslator.LinqNamespace);
            var compFor = (CompFor) sc.Collection;
            var v = compFor.projection?.Accept(this);
            var c = TranslateToLinq(v, compFor);
                return m.Appl(
                    m.MethodRef(
                        c,
                            "ToHashSet"));
            }

        public CodeExpression VisitUnary(UnaryExp u)
        {
            if (u.Operator == Op.Sub && u.Exp is RealLiteral real)
            {
                return new RealLiteral("-" + real.Value, -real.NumericValue, real.Filename, real.Start, real.End).Accept(this);
            }
            var e = u.Exp.Accept(this);
            return new CodeUnaryOperatorExpression(mppyoptocsop[u.Operator], e);
        }

        public CodeExpression VisitBigLiteral(BigLiteral bigLiteral)
        {
            return m.Prim(bigLiteral.Value);
        }

        public CodeExpression VisitBinExp(BinExp bin)
        {
            if (bin.Operator == Op.Mod &&
                (bin.Right is PyTuple || bin.Left is Str))
            {
                return TranslateFormatExpression(bin.Left, bin.Right);
            }

            var l = bin.Left.Accept(this);
            var r = bin.Right.Accept(this);
            switch (bin.Operator)
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
            switch (bin.Operator)
            {
            case Op.Is:
                if (bin.Right is NoneExp)
                {
                    return m.BinOp(l, CodeOperatorType.Is, r);
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
                if (bin.Right is NoneExp)
                {
                    return m.BinOp(l, CodeOperatorType.IsNot, r);
                }
                else
                {
                    return m.BinOp(l, CodeOperatorType.IdentityInequality, r);
                }
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
            case Op.Lt:
            case Op.Gt:
            case Op.Ge:
            case Op.Le:
                if (l is CodeBinaryOperatorExpression binL && IsComparison(binL))
                {
                    return FuseComparisons(binL, bin.Operator, r);
                }
                break;
                // C# has no standard matrix multiplication library,
                // so we emit a function call and let postprocessors 
                // clean it up.
            case Op.MatMul:
                return m.Appl(m.MethodRef(l, "__matmul__"), r);
            case Op.AugMatMul:
                return m.Appl(m.MethodRef(l, "__imatmul__"), r);
            }
            return m.BinOp(l, mppyoptocsop[bin.Operator], r);
        }

        private CodeExpression FuseComparisons(CodeBinaryOperatorExpression binL, Op op, CodeExpression r)
        {
            if (binL.Right is not CodeVariableReferenceExpression variable)
            {
                // Python https://docs.python.org/3/reference/expressions.html#comparisons
                // States that the second expression in a comparison chain is only evaluated once.
                variable = gensym.GenSymLocal("_tmp_", m.TypeRef(typeof(object)));
                m.Assign(variable, binL.Right);
                binL = m.BinOp(binL.Left, binL.Operator, variable);
            }
            var newR = m.BinOp(variable, mppyoptocsop[op], r);
            return m.BinOp(binL, CodeOperatorType.LogAnd, newR);
        }

        private static bool IsComparison(CodeBinaryOperatorExpression bin)
        {
            return comparisons.Contains(bin.Operator);
        }

        private CodeExpression? CondenseComplexConstant(CodeExpression l, CodeExpression r, double imScale)
        {
            if (r is CodePrimitiveExpression primR && primR.Value is Complex complexR)
            {
                if (l is CodePrimitiveExpression primL)
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
                else if (l is CodeNumericLiteral litL)
                {
                    var intL = Convert.ToInt64(litL.Literal, CultureInfo.InvariantCulture);
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
                args.AddRange(tuple.Values
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

        public CodeExpression VisitListComprehension(ListComprehension lc)
        {
            m.EnsureImport(TypeReferenceTranslator.LinqNamespace);
            var compFor = (CompFor) lc.Collection;
            var v = compFor.projection!.Accept(this);
            var c = TranslateToLinq(v, compFor);
            return m.Appl(m.MethodRef(c, "ToList"));
        }

        public CodeExpression VisitIterableUnpacker(IterableUnpacker unpacker)
        {
            throw new NotImplementedException();
            //var compFor = (CompFor)lc.Collection;
            //var v = compFor.variable.Accept(this);
            //var c = lc.Collection.Accept(this);
            //var l = m.ApplyMethod(c, "ToList");
            //return l;
        }

        public CodeExpression VisitLambda(Lambda l)
        {
            var e = new CodeLambdaExpression(
                l.args.Select(a => a.Name.Accept(this)).ToArray(),
                l.Body.Accept(this));
            return e;
        }

        private CodeExpression TranslateToLinq(CodeExpression? projection, CompFor compFor)
        {
            var e = compFor.collection.Accept(this);
            var queryClauses = new List<CodeQueryClause>();
            From(compFor.variable, e, queryClauses);
            var iter = compFor.next;
            while (iter != null)
            {
                if (iter is CompIf filter)
                {
                    e = filter.Test.Accept(this);
                    queryClauses.Add(m.Where(e));
                }
                else if (iter is CompFor join)
                {
                    e = join.collection.Accept(this);
                    From(join.variable, e, queryClauses);
                }
                iter = iter.next;
            }
            queryClauses.Add(m.Select(projection!));
            return m.Query(queryClauses.ToArray());
        }


        private void From(Exp variable, CodeExpression collection, List<CodeQueryClause> queryClauses)
        {
            switch (variable)
            {
            case Identifier id:
                {
                queryClauses.Add(m.From(id.Accept(this), collection));
                    break;
                }
            case ExpList expList:
                {
                    var vars = expList.Expressions.Select(v => v.Accept(this)).ToArray();
                    var type = MakeTupleType(expList.Expressions);
                    var it = gensym.GenSymAutomatic("_tup_", type, false);
                queryClauses.Add(m.From(it, collection));
                AddTupleItemsToQueryClauses(it, vars, queryClauses);
                    break;
                }
            case PyTuple tuple:
                {
                var vars = tuple.Values.Select(v => v.Accept(this)).ToArray();
                var type = MakeTupleType(tuple.Values);
                    var it = gensym.GenSymAutomatic("_tup_", type, false);
                queryClauses.Add(m.From(it, collection));
                AddTupleItemsToQueryClauses(it, vars, queryClauses);
                    break;
                }
            default:
                throw new NotImplementedException(variable.GetType().Name);
            }
        }

        private void AddTupleItemsToQueryClauses(
            CodeVariableReferenceExpression tuple, 
            CodeExpression[] vars, 
            List<CodeQueryClause> queryClauses)
        {
            for (int i = 0; i < vars.Length; ++i)
            {
                var l = m.Let(vars[i], m.Access(tuple, $"Item{i + 1}"));
                queryClauses.Add(l);
            }
        }

        private CodeTypeReference MakeTupleType(params Exp[] expressions)
            => MakeTupleType(expressions);

        private CodeTypeReference MakeTupleType(IEnumerable<Exp> expressions)
        {
            //$TODO: make an effort to make this correct.
            return new CodeTypeReference(typeof(object));
        }

        public CodeExpression VisitList(PyList l)
        {
            var (elemType, nms) = types.TranslateListElementType(l);
            m.EnsureImports(nms);

            var elements = l.Initializer;
            if (ContainsStarExp(elements))
            {
                return ExpandIterableExpanders("ListUtils", l.Initializer, elemType);
            }
            else
            {
            return m.ListInitializer(
                    elemType,
                    l.Initializer
                .Where(e => e != null)
                .Select(e => e.Accept(this)));
        }
        }

        private CodeExpression ExpandIterableExpanders(string utilityClassName, List<Exp> l, CodeTypeReference elemType)
        {
            // Found iterable unpackers.
            var seq = new List<CodeExpression>();
            var subseq = new List<CodeExpression>();
            foreach (var item in l)
            {
                CodeExpression exp;
                if (item is StarExp unpacker)
                {
                    if (subseq.Count > 0)
                    {
                        exp = m.NewArray(elemType, subseq.ToArray());
                        seq.Add(exp);
                        subseq.Clear();
                    }
                    exp = unpacker.Expression.Accept(this);
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
                var exp = m.NewArray(elemType, subseq.ToArray());
                seq.Add(exp);
            }

            var unpack = m.MethodRef(m.TypeRefExpr(utilityClassName), "Unpack");
            unpack.TypeReferences.Add(elemType);
            var result = m.Appl(unpack, seq.ToArray());
            return result;
        }

        private static bool ContainsStarExp(List<Exp> elements)
        {
            return elements.OfType<StarExp>().Any();
        }

        public CodeExpression VisitFieldAccess(AttributeAccess acc)
        {
            var exp = acc.Expression.Accept(this);
            return m.Access(exp, acc.FieldName.Name);
        }

        public CodeExpression VisitGeneratorExp(GeneratorExp g)
        {
            m.EnsureImport(TypeReferenceTranslator.LinqNamespace);
            var compFor = (CompFor)g.Collection;
            var v = g.Projection.Accept(this);
            var c = TranslateToLinq(v, compFor);
            return c;
        }

        public CodeExpression VisitIdentifier(Identifier id)
        {
            if (id.Name == "self")
            {
                return m.This();
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
            throw new NodeException(s, "StarExp not implemented yet.");
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
            if (tuple.Values.Count == 0)
            {
                return MakeTupleCreate(m.Prim("<Empty>"));
            }
            else
            {
                //$BUG: tuple elements can be of different types.
                var elemType = m.TypeRef(typeof(object));
                if (ContainsStarExp(tuple.Values))
                {
                    return ExpandIterableExpanders("TupleUtils", tuple.Values, elemType);
                }
                else
                {
                    return MakeTupleCreate(tuple.Values.Select(v => v.Accept(this)).ToArray());
                }
            }
        }

        private CodeExpression MakeTupleCreate(params CodeExpression[] exprs)
        {
            return m.ValueTuple(exprs);
        }

        public CodeExpression VisitYieldExp(YieldExp yieldExp)
        {
            if (yieldExp.Expression == null)
                return m.Prim(null);
            else
                return yieldExp.Expression.Accept(this);
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
            var list = dc.Source.collection.Accept(this);
            switch (dc.Source.variable)
            {
            case ExpList varList:
                {
                    var args = varList.Expressions.Select((e, i) => (e, $"Item{i + 1}"))
                        .ToDictionary(d => d.e, d => d.Item2);
                    var tpl = gensym.GenSymAutomatic("_tup_", null!, false);

                    gensym.PushIdMappings(varList.Expressions.ToDictionary(e => e.ToString(), e => m.Access(tpl, args[e])));

                    var kValue = dc.Key.Accept(this);
                    var vValue = dc.Value.Accept(this);

                    gensym.PopIdMappings();

                    return m.Appl(
                        m.MethodRef(list, "ToDictionary"),
                        m.Lambda(new CodeExpression[] { tpl }, kValue),
                        m.Lambda(new CodeExpression[] { tpl }, vValue));
                }
            case Identifier id:
                {
                    var kValue = dc.Key.Accept(this);
                    var vValue = dc.Value.Accept(this);
                    return m.Appl(
                        m.MethodRef(list, "ToDictionary"),
                        m.Lambda(new CodeExpression[] { id.Accept(this) }, kValue),
                        m.Lambda(new CodeExpression[] { id.Accept(this) }, vValue));
                }
            case PyTuple tuple:
                {
                    var csTuple = tuple.Accept(this);
                    if (tuple.Values.Count != 2)
                    {
                        //TODO: tuples, especially nested tuples, are hard.
                        //$TODO: should use type knowledge about the tuple.
                        var k = dc.Key.Accept(this);
                        var v = dc.Value.Accept(this);

                        var c = TranslateToLinq(m.ValueTuple(k, v), dc.Source);
                        var type = MakeTupleType(tuple.Values);
                        var e = gensym.GenSymAutomatic("_de_", null!, false);
                        return m.Appl(
                            m.MethodRef(c, "ToDictionary"),
                            m.Lambda(new CodeExpression[] { e }, m.Access(e, "Item1")),
                            m.Lambda(new CodeExpression[] { e }, m.Access(e, "Item2")));
                    }

                    var enumvar = gensym.GenSymAutomatic("_de", null!, false);
                    gensym.PushIdMappings(new Dictionary<string, CodeExpression>
                {
                    { tuple.Values[0].ToString(), m.Access(enumvar, "Key") },
                    { tuple.Values[1].ToString(), m.Access(enumvar, "Value") },
                });

                    var kValue = dc.Key.Accept(this);
                    var vValue = dc.Value.Accept(this);

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