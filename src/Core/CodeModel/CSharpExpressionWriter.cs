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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Pytocs.Core.CodeModel
{
    public class CSharpExpressionWriter : ICodeExpressionVisitor
    {
        /// <summary> Precedence in ascending Order </summary>
        enum Precedence
        {
            /// <summary> Minimum Precedence </summary>
            /// <remarks>
            /// Starting with 1 allows to detect uninitialized enum Values with 0.
            /// </remarks>
            Base = 1,
            Assignment,
            Conditional,
            LogicalOr,
            LogicalAnd,
            BitOr,
            BitXor,
            BinAnd,
            Equality,
            Relational,
            Shift,
            Additive,
            Multiplicative,
            Unary,
            Postfix,
            Primary,

        }

        private static Precedence operatorPrecedence(CodeOperatorType op) => op switch
        {
            CodeOperatorType.Complement => Precedence.Unary,
            CodeOperatorType.Index => Precedence.Unary,
            CodeOperatorType.Not => Precedence.Unary,
            CodeOperatorType.Times => Precedence.Multiplicative,
            CodeOperatorType.Divide => Precedence.Multiplicative,
            CodeOperatorType.Mul => Precedence.Multiplicative,
            CodeOperatorType.Div => Precedence.Multiplicative,
            CodeOperatorType.Mod => Precedence.Multiplicative,
            CodeOperatorType.Add => Precedence.Additive,
            CodeOperatorType.Sub => Precedence.Additive,
            CodeOperatorType.Shl => Precedence.Shift,
            CodeOperatorType.Shr => Precedence.Shift,
            CodeOperatorType.Lt => Precedence.Relational,
            CodeOperatorType.Gt => Precedence.Relational,
            CodeOperatorType.Le => Precedence.Relational,
            CodeOperatorType.Ge => Precedence.Relational,
            CodeOperatorType.Equal => Precedence.Equality,
            CodeOperatorType.NotEqual => Precedence.Equality,
            CodeOperatorType.IdentityEquality => Precedence.Equality,
            CodeOperatorType.IdentityInequality => Precedence.Equality,
            CodeOperatorType.Is => Precedence.Equality,
            CodeOperatorType.IsNot => Precedence.Equality,
            CodeOperatorType.BitAnd => Precedence.BinAnd,
            CodeOperatorType.BitXor => Precedence.BitXor,
            CodeOperatorType.BitOr => Precedence.BitOr,
            CodeOperatorType.LogAnd => Precedence.LogicalAnd,
            CodeOperatorType.LogOr => Precedence.LogicalOr,
            CodeOperatorType.Conditional => Precedence.Conditional,
            CodeOperatorType.Assign => Precedence.Assignment,
            CodeOperatorType.AddEq => Precedence.Assignment,
            CodeOperatorType.SubEq => Precedence.Assignment,
            CodeOperatorType.ModEq => Precedence.Assignment,
            CodeOperatorType.MulEq => Precedence.Assignment,
            CodeOperatorType.AndEq => Precedence.Assignment,
            CodeOperatorType.OrEq => Precedence.Assignment,
            CodeOperatorType.DivEq => Precedence.Assignment,
            CodeOperatorType.ShlEq => Precedence.Assignment,
            CodeOperatorType.ShrEq => Precedence.Assignment,
            CodeOperatorType.XorEq => Precedence.Assignment,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

        internal void VisitTypeReference(object propertyType)
        {
            throw new NotImplementedException();
        }

        private IndentingTextWriter writer;
        private Precedence precedence;
        private bool parensIfSamePrecedence;

        public CSharpExpressionWriter(IndentingTextWriter writer)
        {
            this.writer = writer;
            this.precedence = Precedence.Base;
        }

        public void VisitArrayIndexer(CodeArrayIndexerExpression aref)
        {
            Write(aref.TargetObject, Precedence.Primary, false);
            writer.Write("[");
            var sep = "";
            foreach (var sub in aref.Indices)
            {
                writer.Write(sep);
                sep = ",";
                Write(sub, Precedence.Base, false);
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
                Write(e, Precedence.Base, false);
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
            Write(awaitExp.Expression, Precedence.Unary, false);
        }

        public void VisitBase(CodeBaseReferenceExpression _)
        {
            writer.Write("base");
        }

        public void VisitBinary(CodeBinaryOperatorExpression bin)
        {
            var prec = operatorPrecedence(bin.Operator);
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

        public void VisitCast(CodeCastExpression cast)
        {
            bool needParens = (this.precedence > Precedence.Unary);
            if (needParens)
            {
                writer.Write("(");
            }
            writer.Write("(");
            VisitTypeReference(cast.TargetType);
            writer.Write(") ");
            Write(cast.Expression, Precedence.Unary, true);
            if (needParens)
            {
                writer.Write(")");
            }
        }

        private void Write(CodeExpression e, Precedence prec, bool parens)
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
            case CodeOperatorType.Index: return "^";

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
            case CodeOperatorType.IsNot: return "is not";

            case CodeOperatorType.BitAnd: return "&";
            case CodeOperatorType.BitOr: return "|";
            case CodeOperatorType.BitXor: return "^";

            case CodeOperatorType.LogAnd: return "&&";
            case CodeOperatorType.LogOr: return "||";

            case CodeOperatorType.Not: return "!";
            case CodeOperatorType.Assign: return "=";
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
            case CodeOperatorType.AndEq: return "&=";
            case CodeOperatorType.ShlEq: return "<<=";
            case CodeOperatorType.ShrEq: return ">>=";
            case CodeOperatorType.XorEq: return "^=";
            }
        }

        public void VisitCondition(CodeConditionExpression c)
        {
            Write(c.Condition, Precedence.Conditional, false);
            writer.Write(" ? ");
            Write(c.Consequent, Precedence.Conditional, false);
            writer.Write(" : ");
            Write(c.Alternative, Precedence.Conditional, false);
        }

        public void VisitFieldReference(CodeFieldReferenceExpression field)
        {
            Write(field.Expression, Precedence.Postfix, false);
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

        public void VisitDefaultExpression(CodeDefaultExpression _)
        {
            writer.Write("default");
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
                md.Expression!.Accept(this);
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
                sw.WriteStatements(l.Statements!);
            }
        }

        public void VisitMethodReference(CodeMethodReferenceExpression m)
        {
            if (m.TargetObject != null)
            {
                Write(m.TargetObject, Precedence.Postfix, false);
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
                Write(arg.exp2, Precedence.Base, false);
            }
        }

        public void VisitQueryExpression(CodeQueryExpression q)
        {
            bool needParens = (this.precedence > Precedence.Base);
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
                Write(l.Identifier, Precedence.Base, false);
                writer.Write(" = ");
                Write(l.Value, Precedence.Base, false);
                break;
            case CodeWhereClause w:
                writer.Write("where");
                writer.Write(" ");
                Write(w.Condition, Precedence.Base, false);
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

        public void VisitNumericLiteral(CodeNumericLiteral literal)
        {
            writer.Write(literal.Literal);
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
                string? sep = null;
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
            case int _:
                writer.Write(p.Value.ToString()!);
                break;
            case long l:
                writer.Write("{0}L", l);
                break;
            case bool _:
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
            else if (literal.Format)
            {
                writer.Write("$\"");
            }
            else
            {
                writer.Write("\"");
            }

            for (int i = 0; i < literal.Value.Length; ++i)
            {
                var ch = literal.Value[i];
                switch (ch)
                {
                case '\\':
                    if (literal.Raw)
                    {
                        writer.Write(@"\");
                    }
                    else if (literal.Long)
                    {
                        ch = literal.Value[++i];
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
                        ch = literal.Value[++i];
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
            Write(u.Expression, operatorPrecedence(u.Operator), false);
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
            if (t.ArrayRank > 0)
            {
                writer.Write("[{0}]", new string(',', t.ArrayRank - 1));
            }
        }


        private static readonly Dictionary<string, string> csharpTypenames = new Dictionary<string, string>
        {
            { "int", "int" },
            { "long", "long" },
            { "float", "float" },
            { "double", "double" },
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
