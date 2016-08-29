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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.CodeModel
{
    public class CSharpExpressionWriter : ICodeExpressionVisitor
    {
        private const int PrecPrimary = 14;
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
            { CodeOperatorType.MulEq, 1 },
            { CodeOperatorType.AndEq, 1 },
            { CodeOperatorType.OrEq, 1 },
            { CodeOperatorType.DivEq, 1 },
            { CodeOperatorType.ShlEq, PrecAssignment },
            { CodeOperatorType.ShrEq, PrecAssignment },
            { CodeOperatorType.XorEq, PrecAssignment },
        };

        private IndentingTextWriter writer;
        private int precedence;

        public CSharpExpressionWriter(IndentingTextWriter writer)
        {
            this.writer = writer;
            this.precedence = PrecBase;
        }

        public void VisitArrayIndexer(CodeArrayIndexerExpression aref)
        {
            Write(aref.TargetObject, PrecPrimary);
            writer.Write("[");
            var sep = "";
            foreach (var sub in aref.Indices)
            {
                writer.Write(sep);
                sep = ",";
                Write(sub, PrecBase);
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
                Write(e, PrecBase);
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

        public void VisitBinary(CodeBinaryOperatorExpression bin)
        {
            var prec = operatorPrecedence[bin.Operator];
            if (prec < precedence)
            {
                writer.Write("(");
            }
            Write(bin.Left, prec);
            writer.Write(" {0} ", OpToString(bin.Operator));
            Write(bin.Right, prec);
            if (prec < precedence)
            {
                writer.Write(")");
            }
        }

        private void Write(CodeExpression e, int prec)
        {
            var oldPrec = precedence;
            precedence = prec;
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
            Write(c.Condition, PrecConditional);
            writer.Write(" ? ");
            Write(c.Consequent, PrecConditional);
            writer.Write(" : ");
            Write(c.Alternative, PrecConditional);
        }

        public void VisitFieldReference(CodeFieldReferenceExpression field)
        {
            field.Expression.Accept(this);
            writer.Write(".");
            writer.WriteName(field.FieldName);
        }

        public void VisitInitializer(CodeInitializerExpression i)
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
            writer.Write(" => ");
            l.Body.Accept(this);
        }

        public void VisitMethodReference(CodeMethodReferenceExpression m)
        {
            if (m.TargetObject != null)
            {
                m.TargetObject.Accept(this);
                writer.Write(".");
            }
            writer.WriteName(m.MethodName);
        }

        public void VisitNamedArgument(CodeNamedArgument arg)
        {
            arg.exp1.Accept(this);
            if (arg.exp2 != null)
            {
                writer.Write(": ");
                Write(arg.exp2, PrecBase);
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
            VisitTypeReference(c.Type);
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
            else if (p.Value is string)
                WriteStringLiteral(p.Value as string);
            else if (p.Value is int)
                writer.Write(p.Value.ToString());
            else if (p.Value is long)
                writer.Write("{0}L", p.Value);
            else if (p.Value is bool)
                writer.Write((bool)p.Value ? "true" : "false");
            else if (p.Value is double)
            {
                var s = p.Value.ToString();
                if (!s.Contains('.') && !s.Contains('e') && !s.Contains('E'))
                    s += ".0";
                writer.Write(s);
            }
            else if (p.Value is Syntax.Str)
                WriteStringLiteral((Syntax.Str)p.Value);
            else
                throw new NotImplementedException("" + p.Value);
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
            Write(u.Expression, operatorPrecedence[u.Operator]);
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

        private void GenerateTypeName(string typeName)
        {
            if (typeName == "System.Object")
                writer.Write("object");
            else if (typeName == null)
                writer.Write("void");
            else
                writer.WriteName(typeName);
        }

        public void VisitTypeReference(CodeTypeReferenceExpression t)
        {
            GenerateTypeName(t.TypeName);
        }
    }
}
