﻿using Pytocs.Core.CodeModel;
using Pytocs.Core.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pytocs.Core.Translate
{
    /// <summary>
    /// Translate Python intrinsic functions.
    /// </summary>
    public class IntrinsicTranslator
    {
        private ExpTranslator expTranslator;
        private CodeGenerator m;

        private readonly Dictionary<string, Func<Application, CodeExpression[], CodeExpression>> translators;

        public IntrinsicTranslator(ExpTranslator expTranslator)
        {
            this.expTranslator = expTranslator;
            this.m = expTranslator.m;
            this.translators = new Dictionary<string, Func<Application, CodeExpression[], CodeExpression>>
            {
                { "isinstance", Translate_isinstance },
                { "int", Translate_int },
                { "list", Translate_list },
                { "set", Translate_set },
                { "dict", Translate_dict },
                { "len", Translate_len },
                { "sum", Translate_sum },
                { "range", Translate_range },
                { "filter", Translate_filter },
                { "complex", Translate_complex },
                { "float", Translate_float },
                { "sorted", Translate_sorted },
                { "str", Translate_str },
                { "super", Translate_super },
                { "enumerate", Translate_enumerate },
            };
        }

        /// <summary>
        /// Attempt to translate a Python built-in function.
        /// </summary>
        /// <returns>If a built-in function is found, returns a translated expression,
        /// otherwise returns null.
        /// </returns>
        public CodeExpression MaybeTranslate(string id_Name, Application appl, CodeExpression[] args)
        {
            if (!translators.TryGetValue(id_Name, out var func))
                return null;
            return func(appl, args);
        }

        private CodeExpression Translate_isinstance(Application appl, CodeExpression[] args)
        {
            if (appl.args.Count != 2)
                return null;
            List<Exp> types;
            if (appl.args[1].defval is PyTuple tuple)
            {
                types = tuple.values.ToList();
            }
            else
            {
                types = new List<Exp> { appl.args[1].defval };
            }

            var exp = appl.args[0].defval.Accept(expTranslator);
            return types.
                Select(t => m.BinOp(
                    exp,
                    CodeOperatorType.Is,
                    m.TypeRefExpr(t.ToString())))
                .Aggregate((a, b) => m.BinOp(a, CodeOperatorType.LogOr, b));
        }

        private CodeExpression Translate_int(Application appl, CodeExpression[] args)
        {
            m.EnsureImport("System");
            var fn = m.MethodRef(m.TypeRefExpr("Convert"), "ToInt32");
            return m.Appl(fn, args);
        }

        private CodeExpression Translate_list(Application appl, CodeExpression[] args)
        {
            if (args.Length == 0)
            {
                m.EnsureImport("System.Collections.Generic");
                return m.New(m.TypeRef("List", "object"));
            }
            if (args.Length == 1)
            {
                m.EnsureImport("System.Linq");
                var fn = m.MethodRef(args[0], "ToList");
                return m.Appl(fn);
            }
            return null;
        }

        private CodeExpression Translate_set(Application appl, CodeExpression[] args)
        {
            if (args.Length == 0 || args.Length == 1)
            {
                m.EnsureImport("System.Collections.Generic");
                return m.New(
                    m.TypeRef("HashSet", "object"),
                    args);
            }
            return null;
        }

        private CodeExpression Translate_dict(Application appl, CodeExpression[] args)
        {
            if (args.Length == 0)
            {
                m.EnsureImport("System.Collections.Generic");
                return m.New(
                    m.TypeRef("Dictionary", "object", "object"));
            }
            else if (args.All(a => a is CodeNamedArgument))
            {
                m.EnsureImport("System.Collections.Generic");
                var exp = m.New(
                    m.TypeRef("Dictionary", "string", "object"));
                exp.Initializer = new CodeCollectionInitializer(
                    args.Cast<CodeNamedArgument>()
                        .Select(a =>
                            new CodeCollectionInitializer(
                                m.Prim(
                                    ((CodeVariableReferenceExpression)a.exp1).Name),
                                a.exp2))
                        .ToArray());
                return exp;
            }
            else if (args.Length == 1)
            {
                m.EnsureImport("System.Collections.Generic");
                return m.Appl(m.MethodRef(args[0], "ToDictionary"));
            }
            return null;
        }

        private CodeExpression Translate_len(Application appl, CodeExpression[] args)
        {
            if (args.Length == 1)
            {
                var arg = args[0];
                //$TODO: if args is known to be an iterable, but not a collection,
                // using LinQ Count() instead?
                return m.Access(arg, "Count");
            }
            return null;
        }

        private CodeExpression Translate_sum(Application appl, CodeExpression[] args)
        {
            if (args.Length == 1)
            {
                m.EnsureImport("System.Linq");
                var arg = args[0];
                args = new CodeExpression[0];
                var fn = m.Access(arg, "Sum");
                return m.Appl(fn, args);
            }
            return null;
        }

        private CodeExpression Translate_range(Application appl, CodeExpression[] args)
        {
            var argsCount = args.Length;

            if (1 <= argsCount && argsCount <= 3)
            {
                m.EnsureImport("System");
                m.EnsureImport("System.Linq");

                var enumerableRange = m.MethodRef(m.TypeRefExpr("Enumerable"), "Range");
                var mathCeiling = m.MethodRef(m.TypeRefExpr("Math"), "Ceiling");
                var convertToInt32 = m.MethodRef(m.TypeRefExpr("Convert"), "ToInt32");
                var convertToDouble = m.MethodRef(m.TypeRefExpr("Convert"), "ToDouble");

                switch (argsCount)
                {
                    case 1:
                        {
                            // Enumerable.Range(0, count);

                            var startExp = m.Prim(0);
                            var countExp = args[0];

                            return m.Appl(enumerableRange, startExp, countExp);
                        }
                    case 2:
                        {
                            // Enumerable.Range(0, count);

                            var startExp = args[0];
                            var stopExp = args[1];
                            var countExp = m.Sub(stopExp, startExp);

                            return m.Appl(enumerableRange, startExp, countExp);
                        }
                    case 3:
                        {
                            // Enumerable.Range(0, count).Select(x => start + x * step);

                            var startExp = args[0];
                            var stopExp = args[1];
                            var stepExp = args[2];

                            // count = (stop - start) divide_ceiling_by step;
                            var rawCountExp = m.Sub(stopExp, startExp);
                            var rawDoubleCountExp = m.Appl(convertToDouble, rawCountExp);
                            var countDividedExp = m.Div(rawDoubleCountExp, stepExp);
                            var countCeilingExp = m.Appl(mathCeiling, countDividedExp);
                            var countExp = m.Appl(convertToInt32, countCeilingExp);

                            // Enumerable.Range(0, count);
                            var rangeExp = m.Appl(enumerableRange, m.Prim(0), countExp);

                            // x => start + x * step;
                            var lambdaArgExp = expTranslator.gensym.GenSymLocal("_x_", m.TypeRef("object"));
                            var offsetExp = m.Mul(lambdaArgExp, stepExp);
                            var positionExp = m.Add(startExp, offsetExp);
                            var mapLambdaExp = new CodeLambdaExpression(new CodeExpression[] { lambdaArgExp }, positionExp);

                            var selectExt = m.Access(rangeExp, "Select");

                            return m.Appl(selectExt, mapLambdaExp);
                        }
                }
            }

            return null;
        }

        private CodeExpression Translate_filter(Application appl, CodeExpression[] args)
        {
            if (args.Length == 2)
            {
                m.EnsureImport("System.Collections.Generic");
                m.EnsureImport("System.Linq");
                var filter = args[0];
                if (appl.args[0].defval is NoneExp)
                {
                    var formal = expTranslator.gensym.GenSymLocal("_p_", m.TypeRef("object"));
                    filter = m.Lambda(
                        new[] { formal },
                        m.BinOp(formal, CodeOperatorType.NotEqual, m.Prim(null)));
                }
                var fn = m.MethodRef(
                        m.Appl(m.MethodRef(args[1], "Where"), filter),
                        "ToList");
                args = new CodeExpression[0];
                return m.Appl(fn, args);
            }
            return null;
        }

        private CodeExpression Translate_complex(Application appl, CodeExpression[] args)
        {
            if (args.Length == 2)
            {
                m.EnsureImport("System.Numerics");
                return m.New(m.TypeRef("Complex"), args);
            }
            return null;
        }

        private CodeExpression Translate_float(Application appl, CodeExpression[] args)
        {
            if (args[0] is CodePrimitiveExpression c && c.Value is Str str)
            {
                switch (str.s)
                {
                    case "inf":
                    case "+inf":
                    case "Infinity":
                    case "+Infinity":
                        return m.Access(m.TypeRefExpr("double"), "PositiveInfinity");

                    case "-inf":
                    case "-Infinity":
                        return m.Access(m.TypeRefExpr("double"), "NegativeInfinity");
                }
            }
            return null;
        }

        private CodeExpression Translate_sorted(Application appl, CodeExpression[] args)
        {
            if (args.Length == 0)
                return m.Appl(new CodeVariableReferenceExpression("sorted"), args);
            var iter = args[0];
            var cmp = args.Length > 1 && !(args[1] is CodeNamedArgument)
                ? args[1]
                : null;
            var key = args.Length > 2 && !(args[2] is CodeNamedArgument)
                ? args[2]
                : null;
            var rev = args.Length > 3 && !(args[3] is CodeNamedArgument)
                ? args[3]
                : null;
            var namedArgs = args.OfType<CodeNamedArgument>().ToDictionary(
                k => ((CodeVariableReferenceExpression)k.exp1).Name,
                v => v.exp2);

            if (namedArgs.TryGetValue("cmp", out var tmp))
                cmp = tmp;
            if (namedArgs.TryGetValue("key", out tmp))
                key = tmp;
            if (namedArgs.TryGetValue("reverse", out tmp))
                rev = tmp;

            m.EnsureImport("System.Collections.Generic");
            var formal = expTranslator.gensym.GenSymLocal("_p_", m.TypeRef("object"));
            return m.ApplyMethod(
                m.ApplyMethod(
                    iter,
                    IsReverse(rev) ? "OrderByDescending" : "OrderBy",
                    cmp ?? key ?? m.Lambda(new[] { formal }, formal)),
                "ToList");
        }

        private CodeExpression Translate_str(Application appl, CodeExpression[] args)
        {
            switch (args.Length)
            {
                case 1:
                    return m.ApplyMethod(args[0], "ToString");

                case 2:
                case 3:
                    //$TODO: careless about the third arg here.
                    m.EnsureImport("System.Text");
                    var getEncoding = m.ApplyMethod(
                        m.TypeRefExpr("Encoding"),
                        "GetEncoding",
                        args[1]);
                    return m.ApplyMethod(getEncoding, "GetString", args[0]);

                default:
                    throw new NotImplementedException($"str({string.Join<CodeExpression>(",", args)})");
            }
        }

        /// <summary>
        /// Translates a `super(X,Y)` function call.
        /// </summary>
        /// <param name="appl"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private CodeExpression Translate_super(Application appl, CodeExpression[] args)
        {
            if (expTranslator.classDef != null && expTranslator.classDef.args.Count <= 1)
            {
                return m.Base();
            }
            else
            {
                if (args.Length == 0)
                {
                    return m.Base();
                }
                else if (args[0] is CodeVariableReferenceExpression id)
                {
                    var cast = m.Cast(new CodeTypeReference(id.Name), m.This());
                    return cast;
                }
                else
                {
                    var cast = m.Cast(new CodeTypeReference(args[0].ToString()), m.This());
                    return cast;
                }
            }
        }

        private CodeExpression Translate_enumerate(Application appl, CodeExpression[] args)
        {
            if (args.Length == 1)
            {
                m.EnsureImport("System.Linq");
                var p = expTranslator.gensym.GenSymLocal("_p_", m.TypeRef("object"));
                var i = expTranslator.gensym.GenSymLocal("_p_", m.TypeRef("int"));
                return m.ApplyMethod(
                    args[0],
                    "Select",
                    m.Lambda(
                        new[] { p, i },
                        m.ApplyMethod(m.TypeRefExpr("Tuple"), "Create", i, p)));
            }
            return null;
        }

        private bool IsReverse(CodeExpression rev)
        {
            if (rev == null)
                return false;
            if (rev is CodePrimitiveExpression p)
            {
                if (p.Value is bool)
                {
                    return (bool)p.Value;
                }
            }
            return false;
        }
    }
}