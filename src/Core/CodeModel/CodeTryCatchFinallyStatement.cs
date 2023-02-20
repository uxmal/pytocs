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
using System.Threading.Tasks;

namespace Pytocs.Core.CodeModel
{
    public class CodeTryCatchFinallyStatement : CodeStatement
    {
        public List<CodeStatement> TryStatements { get; private set; }
        public List<CodeCatchClause>    CatchClauses  { get; private set; }
        public List<CodeStatement>    FinallyStatements  { get; private set; }

        public CodeTryCatchFinallyStatement()
        {
            TryStatements = new List<CodeStatement>();
            CatchClauses = new List<CodeCatchClause>();
            FinallyStatements = new List<CodeStatement>();
        }

        public CodeTryCatchFinallyStatement(
            List<CodeStatement> tryStms,
            List<CodeCatchClause> catchClauses,
            List<CodeStatement> finallyStatements)
        {
            this.TryStatements = tryStms;
            this.CatchClauses = catchClauses;
            this.FinallyStatements = finallyStatements;
        }

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.VisitTry(this);
        }
    }
}
