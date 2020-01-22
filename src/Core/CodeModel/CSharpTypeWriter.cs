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

namespace Pytocs.Core.CodeModel
{
    public class CSharpTypeWriter : ICodeMemberVisitor<int>
    {
        private readonly CSharpExpressionWriter expWriter;
        private CodeTypeDeclaration type;
        private readonly IndentingTextWriter writer;

        public CSharpTypeWriter(CodeTypeDeclaration type, IndentingTextWriter writer)
        {
            this.type = type;
            this.writer = writer;
            expWriter = new CSharpExpressionWriter(writer);
        }

        public int VisitField(CodeMemberField field)
        {
            RenderMemberFieldAttributes(field.Attributes);
            CSharpExpressionWriter expWriter = new CSharpExpressionWriter(writer);
            expWriter.VisitTypeReference(field.FieldType);
            writer.Write(" ");
            writer.WriteName(field.FieldName);
            if (field.InitExpression != null)
            {
                writer.Write(" = ");
                field.InitExpression.Accept(expWriter);
            }

            writer.Write(";");
            writer.WriteLine();
            return 0;
        }

        public int VisitProperty(CodeMemberProperty property)
        {
            foreach (CodeCommentStatement comment in property.Comments)
            {
                writer.Write("//");
                writer.WriteLine(comment.Comment);
            }

            RenderCustomAttributes(property);
            RenderMemberFieldAttributes(property.Attributes);
            CSharpExpressionWriter expWriter = new CSharpExpressionWriter(writer);
            expWriter.VisitTypeReference(property.PropertyType);
            writer.Write(" ");
            writer.WriteName(property.Name);
            writer.Write(" ");
            writer.Write("{");
            writer.WriteLine();
            ++writer.IndentLevel;
            writer.Write("get");

            CSharpStatementWriter stmWriter = new CSharpStatementWriter(writer);
            stmWriter.WriteStatements(property.GetStatements);
            writer.WriteLine();

            if (property.SetStatements.Count > 0)
            {
                writer.Write("set");
                stmWriter = new CSharpStatementWriter(writer);
                stmWriter.WriteStatements(property.SetStatements);
                writer.WriteLine();
            }

            --writer.IndentLevel;
            writer.Write("}");
            writer.WriteLine();
            return 0;
        }

        public int VisitMethod(CodeMemberMethod method)
        {
            foreach (CodeCommentStatement comment in method.Comments)
            {
                writer.Write("//");
                writer.WriteLine(comment.Comment);
            }

            RenderCustomAttributes(method);
            RenderMethodAttributes(method);
            CSharpExpressionWriter expWriter = new CSharpExpressionWriter(writer);
            expWriter.VisitTypeReference(method.ReturnType);
            writer.Write(" ");
            writer.WriteName(method.Name);
            WriteMethodParameters(method);

            CSharpStatementWriter stmWriter = new CSharpStatementWriter(writer);
            stmWriter.WriteStatements(method.Statements);
            writer.WriteLine();
            return 0;
        }

        public int VisitConstructor(CodeConstructor cons)
        {
            RenderCustomAttributes(cons);
            RenderMethodAttributes(cons);
            writer.WriteName(type.Name);
            WriteMethodParameters(cons);
            if (cons.BaseConstructorArgs.Count > 0)
            {
                writer.WriteLine();
                ++writer.IndentLevel;
                writer.Write(": ");
                writer.Write("base");
                writer.Write("(");
                string sep = "";
                foreach (CodeExpression e in cons.BaseConstructorArgs)
                {
                    writer.Write(sep);
                    sep = ", ";
                    e.Accept(expWriter);
                }

                writer.Write(")");
                --writer.IndentLevel;
            }

            if (cons.ChainedConstructorArgs.Count > 0)
            {
                writer.WriteLine();
                ++writer.IndentLevel;
                writer.Write(": ");
                writer.Write("this");
                writer.Write("(");
                string sep = "";
                foreach (CodeExpression e in cons.ChainedConstructorArgs)
                {
                    writer.Write(sep);
                    sep = ", ";
                    e.Accept(expWriter);
                }

                writer.Write(")");
                --writer.IndentLevel;
            }

            CSharpStatementWriter stmWriter = new CSharpStatementWriter(writer);
            stmWriter.WriteStatements(cons.Statements);
            writer.WriteLine();
            return 0;
        }

        public int VisitTypeDefinition(CodeTypeDeclaration type)
        {
            CodeTypeDeclaration oldType = this.type;
            this.type = type;
            CSharpExpressionWriter expWriter = new CSharpExpressionWriter(writer);
            foreach (CodeCommentStatement stm in type.Comments)
            {
                writer.Write("//");
                writer.WriteLine(stm.Comment);
            }

            RenderTypeMemberAttributes(type.Attributes);
            writer.Write("class");
            writer.Write(" ");
            writer.WriteName(type.Name);

            if (type.BaseTypes.Count > 0)
            {
                writer.WriteLine();
                ++writer.IndentLevel;
                writer.Write(": ");
                string sepStr = "";
                foreach (CodeTypeReference bt in type.BaseTypes)
                {
                    writer.Write(sepStr);
                    sepStr = ", ";
                    expWriter.VisitTypeReference(bt);
                }

                --writer.IndentLevel;
            }

            writer.Write(" ");
            writer.Write("{");
            writer.WriteLine();
            ++writer.IndentLevel;
            bool sep = true;
            foreach (CodeMember m in type.Members)
            {
                if (sep)
                {
                    writer.WriteLine();
                }

                sep = true;
                m.Accept(this);
            }

            --writer.IndentLevel;
            writer.Write("}");
            writer.WriteLine();
            this.type = oldType;
            return 0;
        }

        private void WriteMethodParameters(CodeMemberMethod method)
        {
            writer.Write("(");
            if (method.Parameters.Count > 4)
            {
                // Poor man's pretty printer
                ++writer.IndentLevel;
                writer.WriteLine();
                for (int i = 0; i < method.Parameters.Count; ++i)
                {
                    WriteParameter(method.Parameters[i]);
                    if (i < method.Parameters.Count - 1)
                    {
                        writer.WriteLine(",");
                    }
                }

                --writer.IndentLevel;
            }
            else
            {
                string sep = "";
                foreach (CodeParameterDeclarationExpression param in method.Parameters)
                {
                    writer.Write(sep);
                    sep = ", ";
                    WriteParameter(param);
                }
            }

            writer.WriteName(")");
        }

        private void WriteParameter(CodeParameterDeclarationExpression param)
        {
            CSharpExpressionWriter expType = new CSharpExpressionWriter(writer);
            if (param.IsVarargs)
            {
                writer.Write("params");
                writer.Write(" ");
                writer.Write("object");
                writer.Write(" [] ");
                writer.WriteName(param.ParameterName);
            }
            else
            {
                expType.VisitTypeReference(param.ParameterType);
                writer.Write(" ");
                writer.WriteName(param.ParameterName);
                if (param.DefaultValue != null)
                {
                    writer.Write(" = ");
                    param.DefaultValue.Accept(expType);
                }
            }
        }

        private void RenderMethodAttributes(CodeMemberMethod method)
        {
            RenderAccessAttributes(method.Attributes);
            if (method.IsAsync)
            {
                writer.Write("async");
                writer.Write(" ");
            }

            switch (method.Attributes & MemberAttributes.ScopeMask)
            {
                case 0:
                    writer.Write("virtual");
                    writer.Write(" ");
                    break;

                case MemberAttributes.Abstract:
                    writer.Write("abstract");
                    writer.Write(" ");
                    break;

                case MemberAttributes.Final: break;
                case MemberAttributes.Static:
                    writer.Write("static");
                    writer.Write(" ");
                    break;

                case MemberAttributes.Override:
                    writer.Write("override");
                    writer.Write(" ");
                    break;

                case MemberAttributes.Const:
                    writer.Write("const");
                    writer.Write(" ");
                    break;
            }
        }

        private void RenderCustomAttributes(CodeMember member)
        {
            foreach (CodeAttributeDeclaration attr in member.CustomAttributes)
            {
                writer.Write("[");
                writer.Write(attr.AttributeType.TypeName);
                if (attr.Arguments.Count > 0)
                {
                    writer.Write("(");
                    string sep = "";
                    foreach (CodeAttributeArgument arg in attr.Arguments)
                    {
                        writer.Write(sep);
                        sep = ",";
                        WriteAttrArgument(arg);
                    }

                    writer.Write(")");
                }

                writer.WriteLine("]");
            }
        }

        private void WriteAttrArgument(CodeAttributeArgument arg)
        {
            if (arg.Name != null)
            {
                writer.Write(arg.Name);
                writer.Write("=");
            }

            arg.Value.Accept(expWriter);
        }

        private void RenderMemberFieldAttributes(MemberAttributes attrs)
        {
            RenderAccessAttributes(attrs);
            switch (attrs & MemberAttributes.ScopeMask)
            {
                case MemberAttributes.Final: break;
                case MemberAttributes.Static:
                    writer.Write("static");
                    writer.Write(" ");
                    break;

                case MemberAttributes.Const:
                    writer.Write("const");
                    writer.Write(" ");
                    break;
            }
        }

        private void RenderAccessAttributes(MemberAttributes attrs)
        {
            switch (attrs & MemberAttributes.AccessMask)
            {
                case MemberAttributes.Private:
                    writer.Write("private");
                    break;

                case MemberAttributes.Family:
                    writer.Write("protected");
                    break;

                case MemberAttributes.Assembly:
                    writer.Write("internal");
                    break;

                case MemberAttributes.Public:
                    writer.Write("public");
                    break;

                default: return;
            }

            writer.Write(" ");
        }

        private void RenderTypeMemberAttributes(MemberAttributes attrs)
        {
            RenderAccessAttributes(attrs);
            switch (attrs & MemberAttributes.ScopeMask)
            {
                case MemberAttributes.Final: break;
                case MemberAttributes.Static:
                    writer.Write("static");
                    writer.Write(" ");
                    break;
            }
        }
    }
}