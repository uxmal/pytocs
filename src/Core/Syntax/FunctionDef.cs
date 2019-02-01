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
using System.IO;
using System.Linq;
using System.Text;

namespace Pytocs.Core.Syntax
{
    public class FunctionDef : Statement
    {
        public readonly Identifier name;
        public readonly List<Parameter> parameters;
        public readonly Exp annotation;
        public readonly SuiteStatement body;
        public bool called = false;         //$ move to big state
        public readonly Identifier vararg;
        public readonly Identifier kwarg;

        public FunctionDef(
            Identifier name, 
            List<Parameter> parameters,
            Identifier vararg,
            Identifier kwarg,
            Exp annotation,
            SuiteStatement body, string filename, int start, int end) 
            : base(filename, start, end) 
        {
            this.name = name;
            this.parameters = parameters;
            this.vararg = vararg;
            this.kwarg = kwarg;
            this.annotation = annotation;
            this.body = body;
        }

        public override void Accept(IStatementVisitor v)
        {
            v.VisitFuncdef(this);
        }

        public override T Accept<T>(IStatementVisitor<T> v)
        {
            return v.VisitFunctionDef(this);
        }

        public bool IsStaticMethod()
        {
            if (decorators == null)
                return false;
            foreach (var d in decorators)
            {
                if (d.Name == "staticmethod")
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsClassMethod()
        {
            if (decorators == null)
                return false;
            foreach (var d in decorators)
            {
                if (d.Name == "classmethod")
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Lambda : Exp
    {
        public List<VarArg> args;
        public Exp body;

        public Lambda(string filename, int start, int end) : base(filename, start, end) { }

        public override T Accept<T>(IExpVisitor<T> v)
        {
            return v.VisitLambda(this);
        }

        public override void Accept(IExpVisitor v)
        {
            v.VisitLambda(this);
        }

        public override void Write(TextWriter writer)
        {
            writer.Write("lambda");
            writer.Write(" ");
            var sep = "";
            foreach (var v in args)
            {
                writer.Write(sep);
                sep = ",";
                if (v.IsIndexed)
                    writer.Write("*");
                else if (v.IsKeyword)
                    writer.Write("**");
                if (v.name != null)
                    v.name.Write(writer);
            }
            writer.Write(":");
            writer.Write(" ");
            this.body.Write(writer);
        }
    }
}
