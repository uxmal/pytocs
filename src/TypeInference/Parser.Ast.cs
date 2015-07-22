using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.yinwang.pysonar.ast;

namespace org.yinwang.pysonar
{
    public partial class Parser
    {
#if NO
    public partial class Parser : Pytocs.Syntax.IStatementVisitor<Node>, Pytocs.Syntax.IExpVisitor<Node>
    {
        private string philename;

        public Node parseModuleStatements(string filename, IEnumerable<Pytocs.Syntax.Statement> statements)
        {
            this.philename = filename;
            var body = new Block(
                statements.Select(s => s.Accept(this)).ToList(),
                philename, 0, 0);
            return new Module( _.moduleName(fs, philename), body, philename, 0, 0); 
        }

        public Node VisitAssert(Pytocs.Syntax.AssertStatement a)
        {
            return new Assert(
                a.Tests[0].Accept(this),
                a.Message.Accept(this),
                philename, 0, 0);
        }

        public Node VisitBreak(Pytocs.Syntax.BreakStatement b)
        {
            return new Break(philename, 0, 0);
        }

        public Node VisitClass(Pytocs.Syntax.ClassDef c)
        {
            return new ClassDef(
                new Name(c.name, philename, 0, 0),
                c.args.Select(a => a.name.Accept(this)).ToList(),
                c.body.Accept(this),
                philename, 0, 0);
        }

        public Node VisitComment(Pytocs.Syntax.CommentStatement c)
        {
            return new Dummy(philename, 0, 0);
        }

        public Node VisitContinue(Pytocs.Syntax.ContinueStatement c)
        {
            return new Continue(philename, 0, 0);
        }

        public Node VisitDecorated(Pytocs.Syntax.Decorated d)
        {
            throw new NotImplementedException();
        }

        public Node VisitDel(Pytocs.Syntax.DelStatement d)
        {
            return new Delete(
                d.Expressions.Select(e => e.Accept(this)).ToList(),
                philename, 0, 0);
        }

        public Node VisitExec(Pytocs.Syntax.ExecStatement exec)
        {
            return new Exec(
                exec.code.Accept(this),
                exec.globals.Accept(this),
                exec.locals.Accept(this),
                philename, 0, 0);
        }

        public Node VisitExp(Pytocs.Syntax.ExpStatement e)
        {
            return new Expr(
                e.Expression.Accept(this),
                philename, 0, 0);
        }

        public Node VisitFor(Pytocs.Syntax.ForStatement f)
        {
            throw new NotImplementedException();
        }

        public Node VisitFrom(Pytocs.Syntax.FromStatement f)
        {
            return new ImportFrom(
                new List<Name>{new Name(f.DottedName.ToString())},
                null,
                0,
                philename, 0, 0);
        }

        public Node VisitFuncdef(Pytocs.Syntax.Funcdef f)
        {
            return new FunctionDef(
                new Name(f.name.Name),
                null,
                null,
                null,
                null,
                null,
                philename, 0, 0);
        }

        public Node VisitGlobal(Pytocs.Syntax.GlobalStatement g)
        {
            return new Global(
                g.names.Select(n => new Name(n)).ToList(),
                philename, 0, 0);
        }

        public Node VisitIf(Pytocs.Syntax.IfStatement i)
        {
            return new If(
                i.Test.Accept(this),
                i.Then.Accept(this),
                i.Else.Accept(this),
                philename, 0, 0);
        }

        public Node VisitImport(Pytocs.Syntax.ImportStatement i)
        {
            return new Import(
                null,
                philename, 0, 0);
        }

        public Node VisitNonLocal(Pytocs.Syntax.NonlocalStatement n)
        {
            return new Global(new List<Name> { new Name("NILZ") }, philename, 0, 0);
        }

        public Node VisitPass(Pytocs.Syntax.PassStatement p)
        {
            return new Pass(philename, 0, 0);
        }

        public Node VisitPrint(Pytocs.Syntax.PrintStatement p)
        {
            return new Print(
                p.outputStream.Accept(this),
                p.args.Select(a => a.defval.Accept(this)).ToList(),
                philename, 0, 0);
        }

        public Node VisitRaise(Pytocs.Syntax.RaiseStatement r)
        {
            return new Raise(
                r.exToRaise.Accept(this),
                null,
                null,
                philename, 0, 0);
        }

        public Node VisitReturn(Pytocs.Syntax.ReturnStatement r)
        {
            return new Return(
                r.Expression != null ? r.Expression.Accept(this):  null,
                philename, 0, 0);
        }

        public Node VisitSuite(Pytocs.Syntax.SuiteStatement s)
        {
            return new Block(
                s.stmts.Select(stm => stm.Accept(this)).ToList(),
                philename, 0, 0);
        }

        public Node VisitTry(Pytocs.Syntax.TryStatement t)
        {
            return new Try(
                t.exHandlers.Select(h => new Handler(
                   null, //  h.ec.exp.Accept(this),
                    null,
                    (Block) h.handler.Accept(this),
                    philename, 0, 0)).ToList(),
                (Block) t.body.Accept(this),
                (Block) t.elseHandler.Accept(this),
                (Block) t.finallyHandler.Accept(this),
                philename, 0, 0);
        }

        public Node VisitWhile(Pytocs.Syntax.WhileStatement w)
        {
            return new While(
                w.Test.Accept(this),
                w.Body.Accept(this),
                w.Else.Accept(this),
                philename, 0, 0);
        }

        public Node VisitWith(Pytocs.Syntax.WithStatement w)
        {
            throw new NotImplementedException();
        }

        public Node VisitYield(Pytocs.Syntax.YieldStatement y)
        {
            return new Yield(
                y.Expression.Accept(this),
                philename, 0, 0);
        }

        public Node VisitAliasedExp(Pytocs.Syntax.AliasedExp a)
        {
            //$TODO
            throw new NotImplementedException();
        }

        public Node VisitApplication(Pytocs.Syntax.Application appl)
        {
            return new Call(
                appl.fn.Accept(this),
                appl.args.Select(a => a.name.Accept(this)).ToList(),
                null,
                null,
                null,
                philename, 0, 0);

            throw new NotImplementedException();
        }

        public Node VisitArrayRef(Pytocs.Syntax.ArrayRef arrayRef)
        {
            throw new NotImplementedException();
        }

        public Node VisitAssignExp(Pytocs.Syntax.AssignExp assignExp)
        {
            return new Assign(
                assignExp.Dst.Accept(this),
                assignExp.Src.Accept(this),
                philename, 0, 0);
        }

        public Node VisitBinExp(Pytocs.Syntax.BinExp bin)
        {
            throw new NotImplementedException();
        }

        public Node VisitBooleanLiteral(Pytocs.Syntax.BooleanLiteral b)
        {
            throw new NotImplementedException();
        }

        public Node VisitCompFor(Pytocs.Syntax.CompFor compFor)
        {
            throw new NotImplementedException();
        }

        public Node VisitCompIf(Pytocs.Syntax.CompIf compIf)
        {
            throw new NotImplementedException();
        }

        public Node VisitDottedName(Pytocs.Syntax.DottedName dot)
        {
            throw new NotImplementedException();
        }

        public Node VisitEllipsis(Pytocs.Syntax.Ellipsis e)
        {
            throw new NotImplementedException();
        }

        public Node VisitExpList(Pytocs.Syntax.ExpList list)
        {
            throw new NotImplementedException();
        }

        public Node VisitFieldAccess(Pytocs.Syntax.FieldAccess acc)
        {
            throw new NotImplementedException();
        }

        public Node VisitIdentifier(Pytocs.Syntax.Identifier id)
        {
            return new Name(id.Name,
                philename, 0, 0);
        }

        public Node VisitImaginary(Pytocs.Syntax.ImaginaryLiteral im)
        {
            throw new NotImplementedException();
        }

        public Node VisitIntLiteral(Pytocs.Syntax.IntLiteral s)
        {
            throw new NotImplementedException();
        }

        public Node VisitLambda(Pytocs.Syntax.Lambda lambda)
        {
            throw new NotImplementedException();
        }

        public Node VisitListComprehension(Pytocs.Syntax.ListComprehension lc)
        {
            throw new NotImplementedException();
        }

        public Node VisitListInitializer(Pytocs.Syntax.ListInitializer l)
        {
            throw new NotImplementedException();
        }

        public Node VisitLongLiteral(Pytocs.Syntax.LongLiteral l)
        {
            throw new NotImplementedException();
        }

        public Node VisitNoneExp()
        {
            throw new NotImplementedException();
        }

        public Node VisitRealLiteral(Pytocs.Syntax.RealLiteral r)
        {
            throw new NotImplementedException();
        }

        public Node VisitSetComprehension(Pytocs.Syntax.SetComprehension set)
        {
            throw new NotImplementedException();
        }

        public Node VisitSetDisplay(Pytocs.Syntax.SetDisplay setDisplay)
        {
            throw new NotImplementedException();
        }

        public Node VisitStarExp(Pytocs.Syntax.StarExp s)
        {
            throw new NotImplementedException();
        }

        public Node VisitStringLiteral(Pytocs.Syntax.StringLiteral s)
        {
            return new Str(s.Value, philename, 0, 0);
        }

        public Node VisitTest(Pytocs.Syntax.Test test)
        {
            throw new NotImplementedException();
        }

        public Node VisitTuple(Pytocs.Syntax.PythonTuple tuple)
        {
            throw new NotImplementedException();
        }

        public Node VisitUnary(Pytocs.Syntax.UnaryExp u)
        {
            throw new NotImplementedException();
        }


        public Node VisitYieldExp(Pytocs.Syntax.YieldExp yieldExp)
        {
            throw new NotImplementedException();
        }

        public Node VisitYieldFromExp(Pytocs.Syntax.YieldFromExp yieldExp)
        {
            throw new NotImplementedException();
        }
    }
#endif

#if NO
            public Node convert(Object o)
        {
            if (!(o is IDictionary<string, object>))
            {
                return null;
            }

            IDictionary<String, Object> map = (IDictionary<String, Object>) o;

            String type = (String) map["type"];
            object rstartDouble;
            if (!map.TryGetValue("start", out rstartDouble)) rstartDouble = 0;
            object rendDouble;
            if (!map.TryGetValue("end", out rendDouble)) rendDouble = 1;

            int start = Convert.ToInt32(rstartDouble);
            int end = Convert.ToInt32(rendDouble);

            if (type.Equals("Module"))
            {
                Block b = convertBlock(map["body"]);
                return new Module(_.moduleName(fs, file), b, file, start, end);
            }

            if (type.Equals("alias"))
            {         // lower case alias
                String qname = (String) map["name"];
                List<Name> names = segmentQname(qname, start + "import ".Length, false);
                Name asname = map["asname"] == null ? null : new Name((String) map["asname"]);
                return new Alias(names, asname, file, start, end);
            }

            if (type.Equals("Assert"))
            {
                Node test = convert(map["test"]);
                Node msg = convert(map["msg"]);
                return new Assert(test, msg, file, start, end);
            }

            // assign could be x=y=z=1
            // turn it into one or more Assign nodes
            // z = 1; y = z; x = z
            if (type.Equals("Assign"))
            {
                List<Node> targets = convertList<Node>(map["targets"]);
                Node value = convert(map["value"]);
                if (targets.Count == 1)
                {
                    return new Assign(targets[0], value, file, start, end);
                }
                else
                {
                    List<Node> assignments = new List<Node>();
                    Node lastTarget = targets[targets.Count - 1];
                    assignments.Add(new Assign(lastTarget, value, file, start, end));

                    for (int i = targets.Count - 2; i >= 0; i--)
                    {
                        Node nextAssign = new Assign(targets[i], lastTarget, file, start, end);
                        assignments.Add(nextAssign);
                    }
                    return new Block(assignments, file, start, end);
                }
            }

            if (type.Equals("Attribute"))
            {
                Node value = convert(map["value"]);
                Name attr = (Name) convert(map["attr_name"]);
                if (attr == null)
                {
                    attr = new Name((String) map["attr"]);
                }
                return new Attribute(value, attr, file, start, end);
            }

            if (type.Equals("AugAssign"))
            {
                Node target = convert(map["target"]);
                Node value = convert(map["value"]);
                Op op = convertOp(map["op"]);
                Node operation = new BinOp(op, target, value, file, target.start, value.end);
                return new Assign(target, operation, file, start, end);
            }

            if (type.Equals("BinOp"))
            {
                Node left = convert(map["left"]);
                Node right = convert(map["right"]);
                Op op = convertOp(map["op"]);

                // desugar complex operators
                if (op == Op.NotEqual)
                {
                    Node eq = new BinOp(Op.Equal, left, right, file, start, end);
                    return new UnaryOp(Op.Not, eq, file, start, end);
                }

                if (op == Op.LtE)
                {
                    Node lt = new BinOp(Op.Lt, left, right, file, start, end);
                    Node eq = new BinOp(Op.Eq, left, right, file, start, end);
                    return new BinOp(Op.Or, lt, eq, file, start, end);
                }

                if (op == Op.GtE)
                {
                    Node gt = new BinOp(Op.Gt, left, right, file, start, end);
                    Node eq = new BinOp(Op.Eq, left, right, file, start, end);
                    return new BinOp(Op.Or, gt, eq, file, start, end);
                }

                if (op == Op.NotIn)
                {
                    Node @in = new BinOp(Op.In, left, right, file, start, end);
                    return new UnaryOp(Op.Not, @in, file, start, end);
                }

                if (op == Op.NotEq)
                {
                    Node @in = new BinOp(Op.Eq, left, right, file, start, end);
                    return new UnaryOp(Op.Not, @in, file, start, end);
                }
                return new BinOp(op, left, right, file, start, end);
            }

            if (type.Equals("BoolOp"))
            {
                List<Node> values = convertList<Node>(map["values"]);
                if (values == null || values.Count < 2)
                {
                    _.die("impossible number of arguments, please fix the Python parser");
                }
                Op op = convertOp(map["op"]);
                BinOp ret = new BinOp(op, values[0], values[1], file, start, end);
                for (int i = 2; i < values.Count; i++)
                {
                    ret = new BinOp(op, ret, values[i], file, start, end);
                }
                return ret;
            }

            if (type.Equals("Bytes"))
            {
                Object s = map["s"];
                return new Bytes(s, file, start, end);
            }

            if (type.Equals("Call"))
            {
                Node func = convert(map["func"]);
                List<Node> args = convertList<Node>(map["args"]);
                List<Keyword> keywords = convertList<Keyword>(map["keywords"]);
                Node kwargs = convert(map["kwarg"]);
                Node starargs = convert(map["starargs"]);
                return new Call(func, args, keywords, kwargs, starargs, file, start, end);
            }

            if (type.Equals("ClassDef"))
            {
                Name name = (Name) convert(map["name_node"]);      // hack
                List<Node> bases = convertList<Node>(map["bases"]);
                Block body = convertBlock(map["body"]);
                return new ClassDef(name, bases, body, file, start, end);
            }

            // left-fold Compare into
            if (type.Equals("Compare"))
            {
                Node left = convert(map["left"]);
                List<Op> ops = convertListOp(map["ops"]);
                List<Node> comparators = convertList<Node>(map["comparators"]);
                Node result = new BinOp(ops[0], left, comparators[0], file, start, end);
                for (int i = 1; i < comparators.Count; i++)
                {
                    Node compNext = new BinOp(ops[i], comparators[i - 1], comparators[i], file, start, end);
                    result = new BinOp(Op.And, result, compNext, file, start, end);
                }
                return result;
            }

            if (type.Equals("comprehension"))
            {
                Node target = convert(map["target"]);
                Node iter = convert(map["iter"]);
                List<Node> ifs = convertList<Node>(map["ifs"]);
                return new Comprehension(target, iter, ifs, file, start, end);
            }

            if (type.Equals("Break"))
            {
                return new Break(file, start, end);
            }

            if (type.Equals("Continue"))
            {
                return new Continue(file, start, end);
            }

            if (type.Equals("Delete"))
            {
                List<Node> targets = convertList<Node>(map["targets"]);
                return new Delete(targets, file, start, end);
            }

            if (type.Equals("Dict"))
            {
                List<Node> keys = convertList<Node>(map["keys"]);
                List<Node> values = convertList<Node>(map["values"]);
                return new Dict(keys, values, file, start, end);
            }

            if (type.Equals("DictComp"))
            {
                Node key = convert(map["key"]);
                Node value = convert(map["value"]);
                List<Comprehension> generators = convertList<Comprehension>(map["generators"]);
                return new DictComp(key, value, generators, file, start, end);
            }

            if (type.Equals("Ellipsis"))
            {
                return new Ellipsis(file, start, end);
            }

            if (type.Equals("ExceptHandler"))
            {
                Node exception = convert(map["type"]);
                List<Node> exceptions;

                if (exception != null)
                {
                    exceptions = new List<Node>();
                    exceptions.Add(exception);
                }
                else
                {
                    exceptions = null;
                }

                Node binder = convert(map["name"]);
                Block body = convertBlock(map["body"]);
                return new Handler(exceptions, binder, body, file, start, end);
            }

            if (type.Equals("Exec"))
            {
                Node body = convert(map["body"]);
                Node globals = convert(map["globals"]);
                Node locals = convert(map["locals"]);
                return new Exec(body, globals, locals, file, start, end);
            }

            if (type.Equals("Expr"))
            {
                Node value = convert(map["value"]);
                return new Expr(value, file, start, end);
            }

            if (type.Equals("For"))
            {
                Node target = convert(map["target"]);
                Node iter = convert(map["iter"]);
                Block body = convertBlock(map["body"]);
                Block orelse = convertBlock(map["orelse"]);
                return new For(target, iter, body, orelse, file, start, end);
            }

            if (type.Equals("FunctionDef") || type.Equals("Lambda"))
            {
                Name name = type.Equals("Lambda") ? null : (Name) convert(map["name_node"]);
                var argsMap = (IDictionary<string, object>) map["args"];
                List<Node> args = convertList<Node>(argsMap["args"]);
                List<Node> defaults = convertList<Node>(argsMap["defaults"]);
                Node body = type.Equals("Lambda") ? convert(map["body"]) : convertBlock(map["body"]);

                // handle vararg depending on different python versions
                Name vararg = null;
                Object varargObj = argsMap["vararg"];
                if (varargObj is String)
                {
                    vararg = new Name((String) argsMap["vararg"]);
                }
                else if (varargObj is Hashtable)
                {
                    String argName = (String) ((Hashtable) varargObj)["arg"];
                    vararg = new Name(argName);
                }

                // handle kwarg depending on different python versions
                Name kwarg = null;
                Object kwargObj = argsMap["kwarg"];
                if (kwargObj is String)
                {
                    kwarg = new Name((String) argsMap["kwarg"]);
                }
                else if (kwargObj is Hashtable)
                {
                    String argName = (String) ((Hashtable) kwargObj)["arg"];
                    kwarg = new Name(argName);
                }

                return new FunctionDef(name, args, body, defaults, vararg, kwarg, file, start, end);
            }

            if (type.Equals("GeneratorExp"))
            {
                Node elt = convert(map["elt"]);
                List<Comprehension> generators = convertList<Comprehension>(map["generators"]);
                return new GeneratorExp(elt, generators, file, start, end);
            }

            if (type.Equals("Global"))
            {
                List<String> names = (List<String>) map["names"];
                List<Name> nameNodes = new List<Name>();
                foreach (String name in names)
                {
                    nameNodes.Add(new Name(name));
                }
                return new Global(nameNodes, file, start, end);
            }

            if (type.Equals("Nonlocal"))
            {
                List<String> names = (List<String>) map["names"];
                List<Name> nameNodes = new List<Name>();
                foreach (String name in names)
                {
                    nameNodes.Add(new Name(name));
                }
                return new Global(nameNodes, file, start, end);
            }

            if (type.Equals("If"))
            {
                Node test = convert(map["test"]);
                Block body = convertBlock(map["body"]);
                Block orelse = convertBlock(map["orelse"]);
                return new If(test, body, orelse, file, start, end);
            }

            if (type.Equals("IfExp"))
            {
                Node test = convert(map["test"]);
                Node body = convert(map["body"]);
                Node orelse = convert(map["orelse"]);
                return new IfExp(test, body, orelse, file, start, end);
            }


            if (type.Equals("Import"))
            {
                List<Alias> aliases = convertList<Alias>(map["names"]);
                locateNames(aliases, start);
                return new Import(aliases, file, start, end);
            }

            if (type.Equals("ImportFrom"))
            {
                String module = (String) map["module"];
                int level = Convert.ToInt32((Double) map["level"]);
                List<Name> moduleSeg = module == null ? null : segmentQname(module, start + "from ".Length + level, true);
                List<Alias> names = convertList<Alias>(map["names"]);
                locateNames(names, start);
                return new ImportFrom(moduleSeg, names, level, file, start, end);
            }

            if (type.Equals("Index"))
            {
                Node value = convert(map["value"]);
                return new Index(value, file, start, end);
            }

            if (type.Equals("keyword"))
            {
                String arg = (String) map["arg"];
                Node value = convert(map["value"]);
                return new Keyword(arg, value, file, start, end);
            }

            if (type.Equals("List"))
            {
                List<Node> elts = convertList<Node>(map["elts"]);
                return new PyList(elts, file, start, end);
            }

            if (type.Equals("Starred"))
            { // f(*[1, 2, 3, 4])
                Node value = convert(map["value"]);
                return new Starred(value, file, start, end);
            }

            if (type.Equals("ListComp"))
            {
                Node elt = convert(map["elt"]);
                List<Comprehension> generators = convertList<Comprehension>(map["generators"]);
                return new ListComp(elt, generators, file, start, end);
            }

            if (type.Equals("Name"))
            {
                String id = (String) map["id"];
                return new Name(id, file, start, end);
            }

            if (type.Equals("NameConstant"))
            {
                String strVal;
                Object value = map["value"];
                if (value is Boolean)
                {
                    strVal = ((Boolean) value) ? "true" : "false";
                }
                else if (value is String)
                {
                    strVal = (String) value;
                }
                else
                {
                    _.msg("[WARNING] NameConstant contains unrecognized value: " + value + ", please report issue");
                    strVal = "";
                }
                return new Name(strVal, file, start, end);
            }

            // another name for Name in Python3 func parameters?
            if (type.Equals("arg"))
            {
                String id = (String) map["arg"];
                return new Name(id, file, start, end);
            }

            if (type.Equals("Num"))
            {

                String num_type = (String) map["num_type"];
                if (num_type.Equals("int"))
                {
                    return new PyInt((String) map["n"], file, start, end);
                }
                else if (num_type.Equals("float"))
                {
                    return new PyFloat((String) map["n"], file, start, end);
                }
                else
                {
                    Object real = map["real"];
                    Object imag = map["imag"];

                    if (real is String)
                    {
                        if (real.Equals("Infinity"))
                        {
                            real = Double.PositiveInfinity;
                        }
                        else if (real.Equals("-Infinity"))
                        {
                            real = Double.NegativeInfinity;
                        }
                    }
                    if (imag is String)
                    {
                        if (imag.Equals("Infinity"))
                        {
                            imag = Double.PositiveInfinity;
                        }
                        else if (real.Equals("-Infinity"))
                        {
                            imag = Double.NegativeInfinity;
                        }
                    }
                    return new PyComplex((double) real, (double) imag, file, start, end);
                }
            }

            if (type.Equals("SetComp"))
            {
                Node elt = convert(map["elt"]);
                List<Comprehension> generators = convertList<Comprehension>(map["generators"]);
                return new SetComp(elt, generators, file, start, end);
            }

            if (type.Equals("Pass"))
            {
                return new Pass(file, start, end);
            }

            if (type.Equals("Print"))
            {
                List<Node> values = convertList<Node>(map["values"]);
                Node destination = convert(map["destination"]);
                return new Print(destination, values, file, start, end);
            }

            if (type.Equals("Raise"))
            {
                Node exceptionType = convert(map["type"]);
                Node inst = convert(map["inst"]);
                Node tback = convert(map["tback"]);
                return new Raise(exceptionType, inst, tback, file, start, end);
            }

            if (type.Equals("Repr"))
            {
                Node value = convert(map["value"]);
                return new Repr(value, file, start, end);
            }

            if (type.Equals("Return"))
            {
                Node value = convert(map["value"]);
                return new Return(value, file, start, end);
            }

            if (type.Equals("Set"))
            {
                List<Node> elts = convertList<Node>(map["elts"]);
                return new PySet(elts, file, start, end);
            }

            if (type.Equals("SetComp"))
            {
                Node elt = convert(map["elt"]);
                List<Comprehension> generators = convertList<Comprehension>(map["generators"]);
                return new SetComp(elt, generators, file, start, end);
            }

            if (type.Equals("Slice"))
            {
                Node lower = convert(map["lower"]);
                Node step = convert(map["step"]);
                Node upper = convert(map["upper"]);
                return new Slice(lower, step, upper, file, start, end);
            }

            if (type.Equals("ExtSlice"))
            {
                List<Node> dims = convertList<Node>(map["dims"]);
                return new ExtSlice(dims, file, start, end);
            }

            if (type.Equals("Str"))
            {
                String s = (String) map["s"];
                return new Str(s, file, start, end);
            }

            if (type.Equals("Subscript"))
            {
                Node value = convert(map["value"]);
                Node slice = convert(map["slice"]);
                return new Subscript(value, slice, file, start, end);
            }

            if (type.Equals("Try"))
            {
                Block body = convertBlock(map["body"]);
                Block orelse = convertBlock(map["orelse"]);
                List<Handler> handlers = convertList<Handler>(map["handlers"]);
                Block finalbody = convertBlock(map["finalbody"]);
                return new Try(handlers, body, orelse, finalbody, file, start, end);
            }

            if (type.Equals("TryExcept"))
            {
                Block body = convertBlock(map["body"]);
                Block orelse = convertBlock(map["orelse"]);
                List<Handler> handlers = convertList<Handler>(map["handlers"]);
                return new Try(handlers, body, orelse, null, file, start, end);
            }

            if (type.Equals("TryFinally"))
            {
                Block body = convertBlock(map["body"]);
                Block finalbody = convertBlock(map["finalbody"]);
                return new Try(null, body, null, finalbody, file, start, end);
            }

            if (type.Equals("Tuple"))
            {
                List<Node> elts = convertList<Node>(map["elts"]);
                return new Tuple(elts, file, start, end);
            }

            if (type.Equals("UnaryOp"))
            {
                Op op = convertOp(map["op"]);
                Node operand = convert(map["operand"]);
                return new UnaryOp(op, operand, file, start, end);
            }

            if (type.Equals("While"))
            {
                Node test = convert(map["test"]);
                Block body = convertBlock(map["body"]);
                Block orelse = convertBlock(map["orelse"]);
                return new While(test, body, orelse, file, start, end);
            }

            if (type.Equals("With"))
            {
                List<Withitem> items = new List<Withitem>();

                Node context_expr = convert(map["context_expr"]);
                Node optional_vars = convert(map["optional_vars"]);
                Block body = convertBlock(map["body"]);

                // Python 3 puts context_expr and optional_vars inside "items"
                if (context_expr != null)
                {
                    Withitem item = new Withitem(context_expr, optional_vars, file, -1, -1);
                    items.Add(item);
                }
                else
                {
                    List<IDictionary<string, object>> itemsMap = (List<IDictionary<string, object>>) map["items"];

                    foreach (IDictionary<string, object> m in itemsMap)
                    {
                        context_expr = convert(m["context_expr"]);
                        optional_vars = convert(m["optional_vars"]);
                        Withitem item = new Withitem(context_expr, optional_vars, file, -1, -1);
                        items.Add(item);
                    }
                }

                return new With(items, body, file, start, end);
            }

            if (type.Equals("Yield"))
            {
                Node value = convert(map["value"]);
                return new Yield(value, file, start, end);
            }

            if (type.Equals("YieldFrom"))
            {
                Node value = convert(map["value"]);
                return new Yield(value, file, start, end);
            }

            _.die("[Please report bug]: unexpected ast node: " + map["type"]);

            return null;
        }
        private List<T> convertList<T>(Object o) where T : Node
        {
            if (o == null)
            {
                return null;
            }
            else
            {
                List<IDictionary<string, object>> @in = (List<IDictionary<string, object>>) o;
                List<T> @out = new List<T>();

                foreach (IDictionary<string, object> m in @in)
                {
                    Node n = convert(m);
                    if (n != null)
                    {
                        @out.Add((T) n);
                    }
                }
                return @out;
            }
        }

        // cpython ast doesn't have location information for names in the Alias node, thus we need to locate it here.
        private void locateNames(List<AliasedExp> names, int start)
        {
            foreach (var a in names)
            {
                Name first = a.exp.[0];
                start = content.IndexOf(first.id, start);
                first.Start = start;
                first.End = start + first.id.Length;
                start = first.End;
                if (a.asname != null)
                {
                    start = content.IndexOf(a.asname.id, start);
                    a.asname.Start = start;
                    a.asname.End = start + a.asname.id.Length;
                    a.asname.Filename = file;  // file is missing for asname node
                    start = a.asname.End;
                }
            }
        }

        private List<Node> convertListNode(Object o)
        {
            if (o == null)
            {
                return null;
            }
            else
            {
                List<IDictionary<string, object>> @in = (List<IDictionary<String, Object>>) o;
                List<Node> @out = new List<Node>();

                foreach (IDictionary<String, Object> m in @in)
                {
                    Node n = convert(m);
                    if (n != null)
                    {
                        @out.Add(n);
                    }
                }
                return @out;
            }
        }

        private SuiteStatement convertBlock(Object o)
        {
            if (o == null)
            {
                return null;
            }
            else
            {
                List<Node> body = convertListNode(o);
                if (body == null || body.Count == 0)
                {
                    return null;
                }
                else
                {
                    return new Block(body, file, 0, 0);
                }
            }
        }

        private List<Op> convertListOp(Object o)
        {
            if (o == null)
            {
                return null;
            }
            else
            {
                List<IDictionary<String, Object>> @in = (List<IDictionary<String, Object>>) o;
                List<Op> @out = new List<Op>();

                foreach (IDictionary<string, object> m in @in)
                {
                    Op n = convertOp(m);
                    @out.Add(n);
                }

                return @out;
            }
        }

        public Op convertOp(Object map)
        {
            String type = (String) ((IDictionary<string, object>) map)["type"];

            if (type.Equals("Add") || type.Equals("UAdd"))
            {
                return Op.Add;
            }

            if (type.Equals("Sub") || type.Equals("USub"))
            {
                return Op.Sub;
            }

            if (type.Equals("Mult"))
            {
                return Op.Mul;
            }

            if (type.Equals("Div"))
            {
                return Op.Div;
            }

            if (type.Equals("Pow"))
            {
                return Op.Pow;
            }

            if (type.Equals("Eq"))
            {
                return Op.Equal;
            }

            if (type.Equals("Is"))
            {
                return Op.Eq;
            }

            if (type.Equals("Lt"))
            {
                return Op.Lt;
            }

            if (type.Equals("Gt"))
            {
                return Op.Gt;
            }


            if (type.Equals("BitAnd"))
            {
                return Op.BitAnd;
            }

            if (type.Equals("BitOr"))
            {
                return Op.BitOr;
            }

            if (type.Equals("BitXor"))
            {
                return Op.BitXor;
            }


            if (type.Equals("In"))
            {
                return Op.In;
            }


            if (type.Equals("LShift"))
            {
                return Op.LShift;
            }

            if (type.Equals("FloorDiv"))
            {
                return Op.FloorDiv;
            }

            if (type.Equals("Mod"))
            {
                return Op.Mod;
            }

            if (type.Equals("RShift"))
            {
                return Op.RShift;
            }

            if (type.Equals("Invert"))
            {
                return Op.Invert;
            }

            if (type.Equals("And"))
            {
                return Op.And;
            }

            if (type.Equals("Or"))
            {
                return Op.Or;
            }

            if (type.Equals("Not"))
            {
                return Op.Not;
            }

            if (type.Equals("NotEq"))
            {
                return Op.NotEqual;
            }

            if (type.Equals("IsNot"))
            {
                return Op.NotEq;
            }

            if (type.Equals("LtE"))
            {
                return Op.LtE;
            }

            if (type.Equals("GtE"))
            {
                return Op.GtE;
            }

            if (type.Equals("NotIn"))
            {
                return Op.NotIn;
            }

            _.die("illegal operator: " + type);
            return 0;
        }
#endif
    }
}
