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
using System.Linq;
using System.Text;

namespace Pytocs.Core.CodeModel
{
    public class CSharpTypeWriter : ICodeMemberVisitor<int>
    {
        private CodeTypeDeclaration? type;
        private readonly IndentingTextWriter writer;
        private readonly CSharpExpressionWriter expWriter;

        public CSharpTypeWriter(CodeTypeDeclaration? type, IndentingTextWriter writer)
        {
            this.type = type;
            this.writer = writer;
            this.expWriter = new CSharpExpressionWriter(writer);
        }

        public int VisitField(CodeMemberField field)
        {
            RenderMemberFieldAttributes(field.Attributes);
            var expWriter = new CSharpExpressionWriter(writer);
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
            foreach (var comment in property.Comments)
            {
                writer.Write("//");
                writer.WriteLine(comment.Comment);
            }
            RenderCustomAttributes(property);
            RenderMemberFieldAttributes(property.Attributes);
            var expWriter = new CSharpExpressionWriter(writer);
            expWriter.VisitTypeReference(property.PropertyType!);
            writer.Write(" ");
            writer.WriteName(property.Name!);
            writer.Write(" ");
            writer.Write("{");
            writer.WriteLine();
            ++writer.IndentLevel;
            writer.Write("get");

            var stmWriter = new CSharpStatementWriter(writer);
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
            foreach (var comment in method.Comments)
            {
                writer.Write("//");
                writer.WriteLine(comment.Comment);
            }
            RenderCustomAttributes(method);
            RenderMethodAttributes(method);
            var expWriter = new CSharpExpressionWriter(writer);
            if (method.ReturnType != null)
            {
                expWriter.VisitTypeReference(method.ReturnType);
                writer.Write(" ");
            }
            writer.WriteName(method.Name!);
            WriteMethodParameters(method.Parameters, writer);

            var stmWriter = new CSharpStatementWriter(writer);
            stmWriter.WriteStatements(method.Statements); 
            writer.WriteLine();
            return 0;
        }

        public static void WriteMethodParameters(List<CodeParameterDeclarationExpression> parameters, IndentingTextWriter writer)
        {
            writer.Write("(");
            if (parameters.Count > 4)
            {
                // Poor man's pretty printer
                ++writer.IndentLevel;
                writer.WriteLine();
                for (int i = 0; i < parameters.Count; ++i)
                {
                    WriteParameter(parameters[i], writer);
                    if (i < parameters.Count - 1)
                    {
                        writer.WriteLine(",");
                    }
                }
                --writer.IndentLevel;
            }
            else
            {
                var sep = "";
                foreach (var param in parameters)
                {
                    writer.Write(sep);
                    sep = ", ";
                    WriteParameter(param, writer);
                }
            }
            writer.WriteName(")");
        }

        public int VisitConstructor(CodeConstructor cons)
        {
            RenderCustomAttributes(cons);
            RenderMethodAttributes(cons);
            writer.WriteName(type!.Name!);
            WriteMethodParameters(cons.Parameters, writer);
            if (cons.BaseConstructorArgs.Count > 0)
            {
                writer.WriteLine();
                ++writer.IndentLevel;
                writer.Write(": ");
                writer.Write("base");
                writer.Write("(");
                var sep = "";
                foreach (var e in cons.BaseConstructorArgs)
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
                var sep = "";
                foreach (var e in cons.ChainedConstructorArgs)
                {
                    writer.Write(sep);
                    sep = ", ";
                    e.Accept(expWriter);
                }
                writer.Write(")");
                --writer.IndentLevel;
            }

            var stmWriter = new CSharpStatementWriter(writer);
            stmWriter.WriteStatements(cons.Statements);
            writer.WriteLine();
            return 0;
        }

        public static  void WriteParameter(CodeParameterDeclarationExpression param, IndentingTextWriter writer)
        {
            var expType = new CSharpExpressionWriter(writer);
            if (param.IsVarargs)
            {
                writer.Write("params");
                writer.Write(" ");
                writer.Write("object");
                writer.Write(" [] ");
                writer.WriteName(param.ParameterName!);
            }
            else
            {
                expType.VisitTypeReference(param.ParameterType!);
                writer.Write(" ");
                writer.WriteName(param.ParameterName!);
                if (param.DefaultValue != null)
                {
                    writer.Write(" = ");
                    param.DefaultValue.Accept(expType);
                }
            }
        }

        public int VisitTypeDefinition(CodeTypeDeclaration type)
        {
            var oldType = this.type;
            this.type = type;
            var expWriter = new CSharpExpressionWriter(writer);
            foreach (var stm in type.Comments)
            {
                writer.Write("//");
                writer.WriteLine(stm.Comment);
            }
            RenderTypeMemberAttributes(type.Attributes);
            if (type.IsClass)
            {
                RenderClass(type, expWriter);
            }
            else if (type.IsEnum)
            {
                RenderEnum(type, expWriter);
            }
            else
                throw new NotImplementedException();
            this.type = oldType;
            return 0;
        }

        private void RenderEnum(CodeTypeDeclaration type, CSharpExpressionWriter expWriter)
        {
            writer.Write("enum");
            writer.Write(" ");
            writer.WriteName(type.Name!);
            writer.Write(" ");
            writer.Write("{");
            writer.WriteLine();
            ++writer.IndentLevel;
            bool sep = true;
            foreach (var m in type.Members)
            {
                if (sep) writer.WriteLine();
                sep = true;
                if (m is CodeMemberField f)
                {
                    writer.Write(f.FieldName!);
                    writer.Write(" = ");
                    f.InitExpression?.Accept(expWriter);
                    writer.Write(",");
                    writer.WriteLine();
                }
                else
                {
                    m.Accept(this);
                }
            }
            --writer.IndentLevel;
            writer.Write("}");
            writer.WriteLine();
        }

        private void RenderClass(CodeTypeDeclaration type, CSharpExpressionWriter expWriter)
        {
            writer.Write("class");
            writer.Write(" ");
            writer.WriteName(type.Name!);

            if (type.BaseTypes.Count > 0)
            {
                writer.WriteLine();
                ++writer.IndentLevel;
                writer.Write(": ");
                var sepStr = "";
                foreach (var bt in type.BaseTypes)
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
            foreach (var m in type.Members)
            {
                if (sep) writer.WriteLine();
                sep = true;
                m.Accept(this);
            }
            --writer.IndentLevel;
            writer.Write("}");
            writer.WriteLine();
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
            case 0: writer.Write("virtual"); writer.Write(" "); break;
            case MemberAttributes.Abstract: writer.Write("abstract"); writer.Write(" "); break;
            case MemberAttributes.Final: break;
            case MemberAttributes.Static: writer.Write("static"); writer.Write(" "); break;
            case MemberAttributes.Override: writer.Write("override"); writer.Write(" "); break;
            case MemberAttributes.Const: writer.Write("const"); writer.Write(" "); break;
            }
        }

        private void RenderCustomAttributes(CodeMember member)
        {
            foreach (var attr in member.CustomAttributes)
            {
                writer.Write("[");
                writer.Write(attr.AttributeType!.TypeName);
                if (attr.Arguments!.Count > 0)
                {
                    writer.Write("(");
                    var sep = "";
                    foreach (var arg in attr.Arguments)
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
            arg.Value!.Accept(expWriter);
        }

        private void RenderMemberFieldAttributes(MemberAttributes attrs)
        {
            RenderAccessAttributes(attrs);
            switch (attrs & MemberAttributes.ScopeMask)
            {
            case MemberAttributes.Final: break;
            case MemberAttributes.Static: writer.Write("static"); writer.Write(" "); break;
            case MemberAttributes.Const: writer.Write("const"); writer.Write(" "); break;
            }
        }

        private void RenderAccessAttributes(MemberAttributes attrs)
        {
            switch (attrs & MemberAttributes.AccessMask)
            {
            case MemberAttributes.Private: writer.Write("private"); break;
            case MemberAttributes.Family: writer.Write("protected"); break;
            case MemberAttributes.Assembly: writer.Write("internal"); break;
            case MemberAttributes.Public: writer.Write("public"); break;
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
            case MemberAttributes.Static: writer.Write("static"); writer.Write(" "); break;
            }
        }
    }
}