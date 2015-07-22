using Pytocs.CodeModel;
using Pytocs.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Translate
{
    public class ModuleTranslator : StatementTranslator
    {
        private CodeGenerator gen;

        public ModuleTranslator(CodeGenerator gen) : base(gen, new HashSet<string>())
        {
            this.gen = gen;
        }

        public void Translate(IEnumerable<Statement> statements)
        {
            int c = 0;
            foreach (var s in statements)
            {
                Str lit;
                if (c == 0 && IsStringStatement(s, out lit))
                {
                    GenerateDocComment(lit.s, gen.CurrentNamespace.Comments);
                }
                else
                {
                    s.Accept(this);
                }
                ++c;
            }
        }

        public void GenerateDocComment(string text, List<CodeCommentStatement> comments)
        {
            var lines = text.Replace("\r\n", "\n").Split('\r', '\n').Select(line => new CodeCommentStatement(" " + line));
            comments.AddRange(lines);
        }

        public bool IsStringStatement(Statement s, out Str lit)
        {
            lit = null;
            var strStmt = s as ExpStatement;
            if (strStmt == null)
            {
                var suite = s as SuiteStatement;
                if (suite == null)
                    return false;
                strStmt = suite.stmts[0] as ExpStatement;
                if (strStmt == null)
                    return false;
            }
            lit = strStmt.Expression as Str;
            return lit != null;
        }

        protected override CodeMemberField GenerateField(string name, CodeExpression value)
        {
            var field = base.GenerateField(name, value);
            field.Attributes |= MemberAttributes.Static;
            return field;
        }
    }
}
