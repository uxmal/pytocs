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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.CodeModel
{
    public class CSharpExpressionWriter : ICodeExpressionVisitor
    {
        private const int PrecPrimary = 15;
        private const int PrecPostfix = 14;
        private const int PrecUnary = 13;
        private const int PrecMultplicative = 12;
        private const int PrecAdditive = 11;
        private const int PrecShift = 10;
        private const int PrecRelational = 9;
        private const int PrecEquality = 8;
        private const int PrecBinAnd = 7;
        private const int PrecBitXor = 6;
        private const int PrecBitOr = 5;
        private const int PrecLogicalAnd = 4;
        private const int PrecLogicalOr = 3;
        private const int PrecConditional = 2;
        private const int PrecAssignment = 1;
        private const int PrecBase = 0;

        private static Dictionary<CodeOperatorType, int> operatorPrecedence = new Dictionary<CodeOperatorType, int>
        {
            { CodeOperatorType.Complement, PrecUnary },
            { CodeOperatorType.Not, 13 },

            { CodeOperatorType.Mul, 12 },
            { CodeOperatorType.Div, 12 },
            { CodeOperatorType.Mod, 12 },

            { CodeOperatorType.Add, 11 },
            { CodeOperatorType.Sub, 11 },

            { CodeOperatorType.Shl, 10 },
            { CodeOperatorType.Shr, 10 },

            { CodeOperatorType.Lt, 9 },
            { CodeOperatorType.Gt, 9 },
            { CodeOperatorType.Le, 9 },
            { CodeOperatorType.Ge, 9 },

            { CodeOperatorType.Equal, 8 },
            { CodeOperatorType.NotEqual, 8 },
            { CodeOperatorType.IdentityEquality, 8 },
            { CodeOperatorType.IdentityInequality, 8 },
            { CodeOperatorType.Is, 8 },

            { CodeOperatorType.BitAnd, 7 },

            { CodeOperatorType.BitXor, 6 },

            { CodeOperatorType.BitOr, 5 },

            { CodeOperatorType.LogAnd, 4 },

            { CodeOperatorType.LogOr, 3 },
             
            { CodeOperatorType.Conditional, 2 },

            { CodeOperatorType.Assign, 1 },
            { CodeOperatorType.AddEq, 1 },
            { CodeOperatorType.SubEq, 1 },
            { CodeOperatorType.ModEq, 1 },
            { CodeOperatorType.MulEq, 1 },
            { CodeOperatorType.AndEq, 1 },
            { CodeOperatorType.OrEq, 1 },
            { CodeOperatorType.DivEq, 1 },
            { CodeOperatorType.ShlEq, PrecAssignment },
            { CodeOperatorType.ShrEq, PrecAssignment },
            { CodeOperatorType.XorEq, PrecAssignment },
        };

        internal void VisitTypeReference(object propertyType)
        {
            throw new NotImplementedException();
        }

        private IndentingTextWriter writer;
        private int precedence;
        private bool parensIfSamePrecedence;

        public CSharpExpressionWriter(IndentingTextWriter writer)
        {
            this.writer = writer;
            this.precedence = PrecBase;
        }

        public void VisitArrayIndexer(CodeArrayIndexerExpression aref)
        {
            Write(aref.TargetObject, PrecPrimary, false);
            writer.Write("[");
            var sep = "";
            foreach (var sub in aref.Indices)
            {
                writer.Write(sep);
                sep = ",";
                Write(sub, PrecBase, false);
            }
            writer.Write("]");
        }

        public void VisitApplication(CodeApplicationExpression app)
        {
            app.Method.Accept(this);
            writer.Write("(");
            var sep = "";
            foreach (var e in app.Arguments)
            {
                writer.Write(sep);
                sep = ", ";
                Write(e, PrecBase, false);
            }
            writer.Write(")");
        }

        public void VisitArrayInitializer(CodeArrayCreateExpression arr)
        {
            writer.Write("new");
            writer.Write(" ");
            VisitTypeReference(arr.ElementType);
            if (arr.Initializers.Length == 0)
            {
                writer.Write("[0]");
            }
            else
            {
                writer.Write("[]");
                writer.Write(" {");
                writer.WriteLine();
                ++writer.IndentLevel;
                var sep = ",";
                foreach (var initializer in arr.Initializers)
                {
                    initializer.Accept(this);
                    writer.Write(sep);
                    writer.WriteLine();
                }
                --writer.IndentLevel;
                writer.Write("}");
            }
        }

        public void VisitAwait(CodeAwaitExpression awaitExp)
        {
            writer.Write("await");
            writer.Write(" ");
            Write(awaitExp.Expression, PrecUnary, false);
        }

        public void VisitBinary(CodeBinaryOperatorExpression bin)
        {
            var prec = operatorPrecedence[bin.Operator];
            bool needParens =
                (prec < precedence ||
                prec == precedence && this.parensIfSamePrecedence);
            if (needParens)
            {
                writer.Write("(");
            }
            Write(bin.Left, prec, false);
            writer.Write(" {0} ", OpToString(bin.Operator));
            Write(bin.Right, prec, true);
            if (needParens)
            {
                writer.Write(")");
            }
        }

        private void Write(CodeExpression e, int prec, bool parens)
        {
            var oldPrec = precedence;
            var oldParens = this.parensIfSamePrecedence;
            precedence = prec;
            parensIfSamePrecedence = parens;
            e.Accept(this);
            precedence = oldPrec;
        }

        private string OpToString(CodeOperatorType codeOperatorType)
        {
            switch (codeOperatorType)
            {
            default: throw new NotImplementedException("Op: " + codeOperatorType);
            case CodeOperatorType.Complement: return "~";

            case CodeOperatorType.Mod: return "%";

            case CodeOperatorType.Add: return "+";
            case CodeOperatorType.Sub: return "-";

            case CodeOperatorType.Gt: return ">";
            case CodeOperatorType.Ge: return ">=";
            case CodeOperatorType.Le: return "<=";
            case CodeOperatorType.Lt: return "<";

            case CodeOperatorType.IdentityEquality: return "==";
            case CodeOperatorType.Equal: return "==";
            case CodeOperatorType.IdentityInequality: return "!=";
            case CodeOperatorType.NotEqual: return "!=";
            case CodeOperatorType.Is: return "is";

            case CodeOperatorType.BitAnd: return "&";
            case CodeOperatorType.BitOr: return "|";
            case CodeOperatorType.BitXor: return "^";

            case CodeOperatorType.LogAnd: return "&&";
            case CodeOperatorType.LogOr: return "||";

            case CodeOperatorType.Not: return "!";
            case CodeOperatorType.Assign: return ":=";
            case CodeOperatorType.Mul: return "*";
            case CodeOperatorType.Div: return "/";
            case CodeOperatorType.Shl: return "<<";
            case CodeOperatorType.Shr: return ">>";

            case CodeOperatorType.AddEq: return "+=";
            case CodeOperatorType.SubEq: return "-=";
            case CodeOperatorType.ModEq: return "%=";
            case CodeOperatorType.MulEq: return "*=";
            case CodeOperatorType.DivEq: return "/=";
            case CodeOperatorType.OrEq: return "|=";
            case CodeOperatorType.AndEq: return "|=";
            case CodeOperatorType.ShlEq: return "<<=";
            case CodeOperatorType.ShrEq: return ">>=";
            case CodeOperatorType.XorEq: return "^=";
            }
        }

        public void VisitCondition(CodeConditionExpression c)
        {
            Write(c.Condition, PrecConditional, false);
            writer.Write(" ? ");
            Write(c.Consequent, PrecConditional, false);
            writer.Write(" : ");
            Write(c.Alternative, PrecConditional, false);
        }

        public void VisitFieldReference(CodeFieldReferenceExpression field)
        {
            Write(field.Expression, PrecPostfix, false);
            writer.Write(".");
            writer.WriteName(field.FieldName);
        }

        public void VisitCollectionInitializer(CodeCollectionInitializer i)
        {
            writer.Write("{");
            writer.WriteLine();
            ++writer.IndentLevel;

            bool sep = false;
            foreach (var v in i.Values)
            {
                if (sep)
                {
                    writer.Write(",");
                    writer.WriteLine();
                }
                sep = true;
                v.Accept(this);
            }
            --writer.IndentLevel;
            writer.Write("}");
        }

        public void VisitObjectInitializer(CodeObjectInitializer i)
        {
            writer.Write("new");
            writer.Write(" ");
            writer.Write("{");
            writer.WriteLine();
            ++writer.IndentLevel;

            bool sep = false;
            foreach (var md in i.MemberDeclarators)
            {
                if (sep)
                {
                    writer.Write(",");
                    writer.WriteLine();
                }
                sep = true;
                if (md.Name != null)
                {
                    writer.Write(md.Name);
                    writer.Write(" = ");
                }
                md.Expression.Accept(this);
            }
            --writer.IndentLevel;
            writer.Write("}");
        }

        public void VisitLambda(CodeLambdaExpression l)
        {
            if (l.Arguments.Length == 1)
            {
                l.Arguments[0].Accept(this);
            }
            else
            {
                writer.Write("(");
                var sep = "";
                foreach (var arg in l.Arguments)
                {
                    writer.Write(sep);
                    sep = ",";
                    arg.Accept(this);
                }
                writer.Write(")");
            }
            writer.Write(" =>");
            if (l.Body != null)
            {
                writer.Write(" ");
                l.Body.Accept(this);
            }
            else
            {
                var sw = new CSharpStatementWriter(writer);
                sw.WriteStatements(l.Statements);
            }
        }

        public void VisitMethodReference(CodeMethodReferenceExpression m)
        {
            if (m.TargetObject != null)
            {
                Write(m.TargetObject, PrecPostfix, false);
                writer.Write(".");
            }
            writer.WriteName(m.MethodName);
            if (m.TypeReferences.Count > 0)
            {
                writer.Write("<");
                var sep = "";
                foreach (var tr in m.TypeReferences)
                {
                    writer.Write(sep);
                    sep = ", ";
                    VisitTypeReference(tr);
                }
                writer.Write(">");
            }
        }

        public void VisitNamedArgument(CodeNamedArgument arg)
        {
            arg.exp1.Accept(this);
            if (arg.exp2 != null)
            {
                writer.Write(": ");
                Write(arg.exp2, PrecBase, false);
            }
        }

        public void VisitQueryExpression(CodeQueryExpression q)
        {
            bool needParens = (this.precedence > PrecBase);
            if (needParens)
            {
                writer.Write("(");
            }
            WriteQueryClause(q.Clauses[0]);

            ++writer.IndentLevel;
            foreach (var clause in q.Clauses.Skip(1))
            {
                writer.WriteLine();
                WriteQueryClause(clause);
            }
            if (needParens)
            {
                writer.Write(")");
            }
            --writer.IndentLevel;
        }

        private void WriteQueryClause(CodeQueryClause clause)
        {
            switch (clause)
            {
            case CodeFromClause f:
                writer.Write("from");
                writer.Write(" ");
                f.Identifier.Accept(this);
                writer.Write(" ");
                writer.Write("in");
                writer.Write(" ");
                f.Collection.Accept(this);
                break;
            case CodeLetClause l:
                writer.Write("let");
                writer.Write(" ");
                Write(l.Identifier, PrecBase, false);
                writer.Write(" = ");
                Write(l.Value, PrecBase, false);
                break;
            case CodeWhereClause w:
                writer.Write("where");
                writer.Write(" ");
                Write(w.Condition, PrecBase, false);
                break;
            case CodeSelectClause s:
                writer.Write("select");
                writer.Write(" ");
                s.Projection.Accept(this);
                break;
            }
        }

        public void VisitThisReference(CodeThisReferenceExpression t)
        {
            writer.Write("this");
        }

        public void VisitVariableReference(CodeVariableReferenceExpression var)
        {
            writer.WriteName(var.Name);
        }

        public void VisitObjectCreation(CodeObjectCreateExpression c)
        {
            writer.Write("new");
            writer.Write(" ");
            if (c.Type != null)
            {
                VisitTypeReference(c.Type);
            }
            if (c.Arguments.Count > 0 || c.Initializers.Count == 0 && c.Initializer == null)
            {
                writer.Write("(");
                var sep = "";
                foreach (var e in c.Arguments)
                {
                    writer.Write(sep);
                    sep = ", ";
                    e.Accept(this);
                }
                writer.Write(")");
            }
            if (c.Initializers.Count > 0)
            {
                writer.Write(" {");
                writer.WriteLine();
                ++writer.IndentLevel;
                string sep = null;
                foreach (var e in c.Initializers)
                {
                    if (sep != null)
                    {
                        writer.Write(sep);
                        writer.WriteLine();
                    }
                    sep = ",";
                    e.Accept(this);
                }
                --writer.IndentLevel;
                writer.WriteLine();
                writer.Write("}");
            }
            if (c.Initializer != null)
            {
                writer.Write(" ");
                c.Initializer.Accept(this);
            }
        }

        public void VisitParameterDeclaration(CodeParameterDeclarationExpression param)
        {
            throw new NotImplementedException();
        }

        public void VisitPrimitive(CodePrimitiveExpression p)
        {
            if (p.Value == null)
                writer.Write("null");
            switch (p.Value)
            {
            case string s:
                WriteStringLiteral(s);
                break;
            case int i:
                writer.Write(p.Value.ToString());
                break;
            case long l:
                writer.Write("{0}L", l);
                break;
            case bool b:
                writer.Write((bool)p.Value ? "true" : "false");
                break;
            case double d:
                WriteReal(d);
                break;
            case BigInteger bigint:
                writer.Write($"new BigInteger({bigint})");
                break;
            case Syntax.Str str:
                WriteStringLiteral(str);
                break;
            case Syntax.Bytes bytes:
                WriteByteLiteral(bytes);
                break;
            case Complex cmp:
                writer.Write("new Complex(");
                WriteReal(cmp.Real);
                writer.Write(", ");
                WriteReal(cmp.Imaginary);
                writer.Write(")");
                break;
            }
        }

        private void WriteReal(double real)
        {
            var dd = real.ToString(CultureInfo.InvariantCulture);
            if (!dd.Contains('.') && !dd.Contains('e') && !dd.Contains('E'))
                dd += ".0";
            writer.Write(dd);
        }

        private void WriteStringLiteral(string literal)
        {
            writer.Write('\"');
            foreach (var ch in literal)
            {
                if (ch == '"')
                    writer.Write('\\');
                writer.Write(ch);
            }
            writer.Write('\"');
        }

        private void WriteByteLiteral(Syntax.Bytes literal)
        {
            writer.Write("new");
            writer.Write(" ");
            writer.Write("byte[]");
            writer.Write(" { ");
            var s = literal.s;
            var sep = "";
            for (int i = 0; i < s.Length; ++i)
            {
                writer.Write(sep);
                sep = ", ";
                if (s[i] == '\\')
                {
                    if (s[i + 1] == 'x')
                    {
                        writer.Write("0x{0}{1}", s[i + 2], s[i + 3]);
                        i += 3;
                    }
                    else if (s[i + 1] == '0')
                    {
                        writer.Write("\\0");
                        i += 1;
                    }
                    else if (s[i + 1] == '\\')
                    {
                        writer.Write("(byte)'\\\\'");
                        i += 1;
                    }
                    else if (s[i + 1] == 'n')
                    {
                        writer.Write("\\n");
                        i += 1;
                    }
                    else if (s[i + 1] == 't')
                    {
                        writer.Write("\\t");
                        i += 1;
                    }
                    else
                    {
                        writer.Write("\\{0}", s[i]);
                        i += 1;
                    }
                }
                else if (' ' <= s[i] && s[i] <= '~')
                {
                   writer.Write("(byte)'{0}'", s[i]);
                }
                else
                {
                    writer.Write("0x{0:X2}", (int)s[i]);
                }
            }
            writer.Write(" }");
        }
        private void WriteStringLiteral(Syntax.Str literal)
        {
            if (literal.Long || literal.Raw)
            {
                writer.Write("@\"");
            }
            else
                writer.Write("\"");

            for (int i = 0; i < literal.s.Length; ++i)
            {
                var ch = literal.s[i];
                switch (ch)
                {
                case '\\':
                    if (literal.Raw)
                    {
                        writer.Write(@"\");
                    }
                    else if (literal.Long)
                    {
                        ch = literal.s[++i];
                        switch (ch)
                        {
                        default:
                            writer.Write("\" + \"");
                            writer.Write("\\{0}", ch);
                            writer.Write("\" +@\"");
                            break;
                        case ' ':
                        case '*':
                        case 'l':
                        case '\r':
                        case '\n':
                            writer.Write("\\{0}", ch);
                            break;
                        case '\"':
                            writer.Write("\"\"");
                            break;
                        case 'u':
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        ch = literal.s[++i];
                        writer.Write(@"\{0}", ch);
                    }
                    break;
                case '\"':
                    if (literal.Long || literal.Raw)
                        writer.Write("\"\"");
                    else
                        writer.Write("\\\"");
                    break;
                default:
                    writer.Write(ch);
                    break;
                }
            }
            writer.Write("\"");
        }

        public void VisitUnary(CodeUnaryOperatorExpression u)
        {
            writer.Write(OpToString(u.Operator));
            Write(u.Expression, operatorPrecedence[u.Operator], false);
        }

        public void VisitTypeReference(CodeTypeReference t)
        {
            GenerateTypeName(t.TypeName);
            if (t.TypeArguments.Count > 0)
            {
                writer.Write("<");
                var sep = "";
                foreach (var ta in t.TypeArguments)
                {
                    writer.Write(sep);
                    sep = ", ";
                    VisitTypeReference(ta);
                }
                writer.Write(">");
            }
        }


        private static Dictionary<string, string> csharpTypenames = new Dictionary<string, string>
        {
            { "int", "int" },
            { "long", "long" },
            { "System.Boolean", "bool" },
            { "System.Double", "double" },
            { "System.Int32", "int" },
            { "System.Object", "object" },
            { "System.String", "string" },
        };

        private void GenerateTypeName(string typeName)
        {
            if (typeName == null)
                writer.Write("void");
            else
            {
                if (csharpTypenames.TryGetValue(typeName, out var csharpName))
                    writer.Write(csharpName);
                else
                    writer.WriteName(typeName);
            }
        }

        public void VisitTypeReference(CodeTypeReferenceExpression t)
        {
            GenerateTypeName(t.TypeName);
        }

        public void VisitValueTuple(CodeValueTupleExpression tuple)
        {
            if (tuple.Expressions.Length <= 1)
            {
                writer.Write("ValueTuple.Create");
                writer.Write("(");
                if (tuple.Expressions.Length == 1)
                {
                    tuple.Expressions[0].Accept(this);
                }
                writer.Write(")");
            }
            else
            {
                writer.Write("(");
                var sep = "";
                foreach (var exp in tuple.Expressions)
                {
                    writer.Write(sep);
                    sep = ", ";
                    exp.Accept(this);
                }
                writer.Write(")");

            }
        }
    }
}
