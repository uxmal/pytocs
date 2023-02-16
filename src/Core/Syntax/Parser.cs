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
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.Core.Syntax
{
    /// <summary>
    /// Parses the Python language grammar
    /// </summary>
    public class Parser
    {
#pragma warning disable IDE1006 // Naming Styles
        #region Python 3.7 grammar
        /*
# Grammar for Python

# NOTE WELL: You should also follow all the steps listed at
# https://devguide.python.org/grammar/

# Start symbols for the grammar:
#       single_input is a single interactive statement;
#       file_input is a module or sequence of commands read from an input file;
#       eval_input is the input for the eval() functions.
# NB: compound_stmt in single_input is followed by extra NEWLINE!
single_input: NEWLINE | simple_stmt | compound_stmt NEWLINE
file_input: (NEWLINE | stmt)* ENDMARKER
eval_input: testlist NEWLINE* ENDMARKER

decorator: '@' dotted_name [ '(' [arglist] ')' ] NEWLINE
decorators: decorator+
decorated: decorators (classdef | funcdef | async_funcdef)

async_funcdef: 'async' funcdef
funcdef: 'def' NAME parameters ['->' test] ':' suite

parameters: '(' [typedargslist] ')'
typedargslist: (tfpdef ['=' test] (',' tfpdef ['=' test])* [',' [
        '*' [tfpdef] (',' tfpdef ['=' test])* [',' ['**' tfpdef [',']]]
      | '**' tfpdef [',']]]
  | '*' [tfpdef] (',' tfpdef ['=' test])* [',' ['**' tfpdef [',']]]
  | '**' tfpdef [','])
tfpdef: NAME [':' test]
varargslist: (vfpdef ['=' test] (',' vfpdef ['=' test])* [',' [
        '*' [vfpdef] (',' vfpdef ['=' test])* [',' ['**' vfpdef [',']]]
      | '**' vfpdef [',']]]
  | '*' [vfpdef] (',' vfpdef ['=' test])* [',' ['**' vfpdef [',']]]
  | '**' vfpdef [',']
)
vfpdef: NAME

stmt: simple_stmt | compound_stmt
simple_stmt: small_stmt (';' small_stmt)* [';'] NEWLINE
small_stmt: (expr_stmt | del_stmt | pass_stmt | flow_stmt |
             import_stmt | global_stmt | nonlocal_stmt | assert_stmt)
expr_stmt: testlist_star_expr (annassign | augassign (yield_expr|testlist) |
                     ('=' (yield_expr|testlist_star_expr))*)
annassign: ':' test ['=' test]
testlist_star_expr: (test|star_expr) (',' (test|star_expr))* [',']
augassign: ('+=' | '-=' | '*=' | '@=' | '/=' | '%=' | '&=' | '|=' | '^=' |
            '<<=' | '>>=' | '**=' | '//=')
# For normal and annotated assignments, additional restrictions enforced by the interpreter
del_stmt: 'del' exprlist
pass_stmt: 'pass'
flow_stmt: break_stmt | continue_stmt | return_stmt | raise_stmt | yield_stmt
break_stmt: 'break'
continue_stmt: 'continue'
return_stmt: 'return' [testlist]
yield_stmt: yield_expr
raise_stmt: 'raise' [test ['from' test]]
import_stmt: import_name | import_from
import_name: 'import' dotted_as_names
# note below: the ('.' | '...') is necessary because '...' is tokenized as ELLIPSIS
import_from: ('from' (('.' | '...')* dotted_name | ('.' | '...')+)
              'import' ('*' | '(' import_as_names ')' | import_as_names))
import_as_name: NAME ['as' NAME]
dotted_as_name: dotted_name ['as' NAME]
import_as_names: import_as_name (',' import_as_name)* [',']
dotted_as_names: dotted_as_name (',' dotted_as_name)*
dotted_name: NAME ('.' NAME)*
global_stmt: 'global' NAME (',' NAME)*
nonlocal_stmt: 'nonlocal' NAME (',' NAME)*
assert_stmt: 'assert' test [',' test]

compound_stmt: if_stmt | while_stmt | for_stmt | try_stmt | with_stmt | funcdef | classdef | decorated | async_stmt
async_stmt: 'async' (funcdef | with_stmt | for_stmt)
if_stmt: 'if' test ':' suite ('elif' test ':' suite)* ['else' ':' suite]
while_stmt: 'while' test ':' suite ['else' ':' suite]
for_stmt: 'for' exprlist 'in' testlist ':' suite ['else' ':' suite]
try_stmt: ('try' ':' suite
           ((except_clause ':' suite)+
            ['else' ':' suite]
            ['finally' ':' suite] |
           'finally' ':' suite))
with_stmt: 'with' with_item (',' with_item)*  ':' suite
with_item: test ['as' expr]
# NB compile.c makes sure that the default except clause is last
except_clause: 'except' [test ['as' NAME]]
suite: simple_stmt | NEWLINE INDENT stmt+ DEDENT

test: or_test ['if' or_test 'else' test] | lambdef
test_nocond: or_test | lambdef_nocond
lambdef: 'lambda' [varargslist] ':' test
lambdef_nocond: 'lambda' [varargslist] ':' test_nocond
or_test: and_test ('or' and_test)*
and_test: not_test ('and' not_test)*
not_test: 'not' not_test | comparison
comparison: expr (comp_op expr)*
# <> isn't actually a valid comparison operator in Python. It's here for the
# sake of a __future__ import described in PEP 401 (which really works :-)
comp_op: '<'|'>'|'=='|'>='|'<='|'<>'|'!='|'in'|'not' 'in'|'is'|'is' 'not'
star_expr: '*' expr
expr: xor_expr ('|' xor_expr)*
xor_expr: and_expr ('^' and_expr)*
and_expr: shift_expr ('&' shift_expr)*
shift_expr: arith_expr (('<<'|'>>') arith_expr)*
arith_expr: term (('+'|'-') term)*
term: factor (('*'|'@'|'/'|'%'|'//') factor)*
factor: ('+'|'-'|'~') factor | power
power: atom_expr ['**' factor]
atom_expr: ['await'] atom trailer*
atom: ('(' [yield_expr|testlist_comp] ')' |
       '[' [testlist_comp] ']' |
       '{' [dictorsetmaker] '}' |
       NAME | NUMBER | STRING+ | '...' | 'None' | 'True' | 'False')
testlist_comp: (test|star_expr) ( comp_for | (',' (test|star_expr))* [','] )
trailer: '(' [arglist] ')' | '[' subscriptlist ']' | '.' NAME
subscriptlist: subscript (',' subscript)* [',']
subscript: test | [test] ':' [test] [sliceop]
sliceop: ':' [test]
exprlist: (expr|star_expr) (',' (expr|star_expr))* [',']
testlist: test (',' test)* [',']
dictorsetmaker: ( ((test ':' test | '**' expr)
                   (comp_for | (',' (test ':' test | '**' expr))* [','])) |
                  ((test | star_expr)
                   (comp_for | (',' (test | star_expr))* [','])) )

classdef: 'class' NAME ['(' [arglist] ')'] ':' suite

arglist: argument (',' argument)*  [',']

# The reason that keywords are test nodes instead of NAME is that using NAME
# results in an ambiguity. ast.c makes sure it's a NAME.
# "test '=' test" is really "keyword '=' test", but we have no such token.
# These need to be in a single rule to avoid grammar that is ambiguous
# to our LL(1) parser. Even though 'test' includes '*expr' in star_expr,
# we explicitly match '*' here, too, to give it proper precedence.
# Illegal combinations and orderings are blocked in ast.c:
# multiple (test comp_for) arguments are blocked; keyword unpackings
# that precede iterable unpackings are blocked; etc.
argument: ( test [comp_for] |
            test '=' test |
            '**' test |
            '*' test )

comp_iter: comp_for | comp_if
sync_comp_for: 'for' exprlist 'in' or_test [comp_iter]
comp_for: ['async'] sync_comp_for
comp_if: 'if' test_nocond [comp_iter]

# not used in grammar, but may appear in "node" passed from Parser to Compiler
encoding_decl: NAME

yield_expr: 'yield' [yield_arg]
yield_arg: 'from' test | testlist
         */
        #endregion

        #region Python 2.7.6 grammar
        /*
# Grammar for Python

# Note:  Changing the grammar specified in this file will most likely
#        require corresponding changes in the parser module
#        (../Modules/parsermodule.c).  If you can't make the changes to
#        that module yourself, please co-ordinate the required changes
#        with someone who can; ask around on python-dev for help.  Fred
#        Drake <fdrake@acm.org> will probably be listening there.

# NOTE WELL: You should also follow all the steps listed in PEP 306,
# "How to Change Python's Grammar"

# Start symbols for the grammar:
#       single_input is a single interactive statement;
#       file_input is a module or sequence of commands read from an input file;
#       eval_input is the input for the eval() and input() functions.
# NB: compound_stmt in single_input is followed by extra NEWLINE!
single_input: NEWLINE | simple_stmt | compound_stmt NEWLINE
file_input: (NEWLINE | stmt)* ENDMARKER
eval_input: testlist NEWLINE* ENDMARKER

decorator: '@' dotted_name [ '(' [arglist] ')' ] NEWLINE
decorators: decorator+
decorated: decorators (classdef | funcdef | async_funcdef)

async_funcdef: 'async' funcdef
funcdef: 'def' NAME parameters ':' suite
parameters: '(' [varargslist] ')'
varargslist: ((fpdef ['=' test] ',')*
              ('*' NAME [',' '**' NAME] | '**' NAME) |
              fpdef ['=' test] (',' fpdef ['=' test])* [','])
fpdef: NAME | '(' fplist ')'
fplist: fpdef (',' fpdef)* [',']

stmt: simple_stmt | compound_stmt
simple_stmt: small_stmt (';' small_stmt)* [';'] NEWLINE
small_stmt: (expr_stmt | print_stmt  | del_stmt | pass_stmt | flow_stmt |
             import_stmt | global_stmt | exec_stmt | assert_stmt)
expr_stmt: testlist (augassign (yield_expr|testlist) |
                     ('=' (yield_expr|testlist))*)
augassign: ('+=' | '-=' | '*=' | '/=' | '%=' | '&=' | '|=' | '^=' |
            '<<=' | '>>=' | '**=' | '//=')
# For normal assignments, additional restrictions enforced by the interpreter
print_stmt: 'print' ( [ test (',' test)* [','] ] |
                      '>>' test [ (',' test)+ [','] ] )
del_stmt: 'del' exprlist
pass_stmt: 'pass'
flow_stmt: break_stmt | continue_stmt | return_stmt | raise_stmt | yield_stmt
break_stmt: 'break'
continue_stmt: 'continue'
return_stmt: 'return' [testlist]
yield_stmt: yield_expr
raise_stmt: 'raise' [test [',' test [',' test]]]
import_stmt: import_name | import_from
import_name: 'import' dotted_as_names
import_from: ('from' ('.'* dotted_name | '.'+)
              'import' ('*' | '(' import_as_names ')' | import_as_names))
import_as_name: NAME ['as' NAME]
dotted_as_name: dotted_name ['as' NAME]
import_as_names: import_as_name (',' import_as_name)* [',']
dotted_as_names: dotted_as_name (',' dotted_as_name)*
dotted_name: NAME ('.' NAME)*
global_stmt: 'global' NAME (',' NAME)*
exec_stmt: 'exec' expr ['in' test [',' test]]
assert_stmt: 'assert' test [',' test]

compound_stmt: if_stmt | while_stmt | for_stmt | try_stmt | with_stmt | funcdef | classdef | decorated | async_stmt
async_stmt: 'async' (funcdef | with_stmt | for_stmt)
if_stmt: 'if' test ':' suite ('elif' test ':' suite)* ['else' ':' suite]
while_stmt: 'while' test ':' suite ['else' ':' suite]
for_stmt: 'for' exprlist 'in' testlist ':' suite ['else' ':' suite]
try_stmt: ('try' ':' suite
           ((except_clause ':' suite)+
            ['else' ':' suite]
            ['finally' ':' suite] |
           'finally' ':' suite))
with_stmt: 'with' with_item (',' with_item)*  ':' suite
with_item: test ['as' expr]
# NB compile.c makes sure that the default except clause is last
except_clause: 'except' [test [('as' | ',') test]]
suite: simple_stmt | NEWLINE INDENT stmt+ DEDENT

# Backward compatibility cruft to support:
# [ x for x in lambda: True, lambda: False if x() ]
# even while also allowing:
# lambda x: 5 if x else 2
# (But not a mix of the two)
testlist_safe: old_test [(',' old_test)+ [',']]
old_test: or_test | old_lambdef
old_lambdef: 'lambda' [varargslist] ':' old_test

test: or_test ['if' or_test 'else' test] | lambdef
or_test: and_test ('or' and_test)*
and_test: not_test ('and' not_test)*
not_test: 'not' not_test | comparison
comparison: expr (comp_op expr)*
comp_op: '<'|'>'|'=='|'>='|'<='|'<>'|'!='|'in'|'not' 'in'|'is'|'is' 'not'
expr: xor_expr ('|' xor_expr)*
xor_expr: and_expr ('^' and_expr)*
and_expr: shift_expr ('&' shift_expr)*
shift_expr: arith_expr (('<<'|'>>') arith_expr)*
arith_expr: term (('+'|'-') term)*
term: factor (('*'|'/'|'%'|'//') factor)*
factor: ('+'|'-'|'~') factor | power
power: atom trailer* ['**' factor]
atom: ('(' [yield_expr|testlist_comp] ')' |
       '[' [listmaker] ']' |
       '{' [dictorsetmaker] '}' |
       '`' testlist1 '`' |
       NAME | NUMBER | STRING+)
listmaker: test ( list_for | (',' test)* [','] )
testlist_comp: test ( comp_for | (',' test)* [','] )
lambdef: 'lambda' [varargslist] ':' test
trailer: '(' [arglist] ')' | '[' subscriptlist ']' | '.' NAME
subscriptlist: subscript (',' subscript)* [',']
subscript: '.' '.' '.' | test | [Fact] ':' [Fact] [sliceop]
sliceop: ':' [Fact]
exprlist: expr (',' expr)* [',']
testlist: test (',' test)* [',']
dictorsetmaker: ( (test ':' test (comp_for | (',' test ':' test)* [','])) |
                  (test (comp_for | (',' test)* [','])) )

classdef: 'class' NAME ['(' [testlist] ')'] ':' suite

arglist: (argument ',')* (argument [',']
                         |'*' test (',' argument)* [',' '**' test] 
                         |'**' test)
# The reason that keywords are test nodes instead of NAME is that using NAME
# results in an ambiguity. ast.c makes sure it's a NAME.
argument: test [comp_for] | test '=' test

list_iter: list_for | list_if
list_for: 'for' exprlist 'in' testlist_safe [list_iter]
list_if: 'if' old_test [list_iter]

comp_iter: comp_for | comp_if
sync_comp_for: 'for' exprlist 'in' or_test [comp_iter]
comp_for: ['async'] sync_comp_for
comp_if: 'if' old_test [comp_iter]

testlist1: test (',' test)*

# not used in grammar, but may appear in "node" passed from Parser to Compiler
encoding_decl: NAME

yield_expr: 'yield' [testlist]
        */
        #endregion

        private readonly string filename;
        private readonly ILexer lexer;
        private readonly bool catchExceptions;
        private readonly ILogger logger;

        public Parser(string filename, ILexer lexer, bool catchExceptions = false, ILogger? logger = null)
        {
            this.filename = filename;
            this.lexer = lexer;
            this.catchExceptions = catchExceptions;
            this.logger = logger ?? NullLogger.Instance;
        }

        public void ParseSingleStatement()
        {
        }

        public IEnumerable<Statement> Parse()
        {
            while (!Peek(TokenType.EOF))
            {
                if (PeekAndDiscard(TokenType.NEWLINE))
                    continue;
                foreach (var stm in stmt())
                {
                    yield return stm;
                }
            }
        }

        public void ParseEval()
        {
        }

        private bool PeekAndDiscard(TokenType tok)
        {
            if (lexer.Peek().Type != tok)
                return false;

            lexer.Get();
            return true;
        }

        private bool PeekAndDiscard(TokenType tok, out Token token)
        {
            if (!Peek(tok, out token))
            {
                return false;
            }

            token = lexer.Get();
            return true;
        }

        private Exception Error(string str, params object [] args)
        {
            str = string.Format(str, args);
            throw new InvalidOperationException($"{filename}({lexer.LineNumber}): {str}");
        }

        private Exception Unexpected()
        {
            return Unexpected(lexer.Peek());
        }

        private Exception Unexpected(Token token)
        {
            return Error(Resources.ErrUnexpectedToken, token); 
        }

#if NEVER
// Grammar for Python
//
// Note:  Changing the grammar specified in this file will most likely
//        require corresponding changes in the parser module
//        (../Modules/parsermodule.c).  If you can't make the changes to
//        that module yourself, please co-ordinate the required changes
//        with someone who can; ask around on python-dev for help.  Fred
//        Drake <fdrake@acm.org> will probably be listening there.
//
// NOTE WELL: You should also follow all the steps listed in PEP 306,
// "How to Change Python's Grammar"
//
// Start symbols for the grammar:
//       single_input is a single interactive statement;
//       file_input is a module or sequence of commands read from an input file;
//       eval_input is the input for the eval() functions.
// NB: compound_stmt in single_input is followed by extra NEWLINE!
single_input: NEWLINE | simple_stmt | compound_stmt NEWLINE
file_input: (NEWLINE | stmt)* ENDMARKER
eval_input: testlist NEWLINE* ENDMARKER
#endif
        //decorator: '@' dotted_name [ '(' [arglist] ')' ] NEWLINE
        private Decorator decorator()
        {
            var posStart = Expect(TokenType.AT).Start;
            var dn = dotted_name();
            List<Argument> args = new List<Argument>();
            if (PeekAndDiscard(TokenType.LPAREN, out var token))
            {
                if (!Peek(TokenType.RPAREN))
                {
                    args = arglist(dn, token.Start).Args;
                }
                Expect(TokenType.RPAREN);
            }
            if (Peek(TokenType.COMMENT))
            {
                //$TODO: do something with this?
                var eolComment = (string?)Expect(TokenType.COMMENT).Value;
            }
            var posEnd = Expect(TokenType.NEWLINE).Start;
            return new Decorator(dn, args, filename, posStart, posEnd);
        }

        private bool Peek(TokenType tokenType)
        {
            return lexer.Peek().Type == tokenType;
        }

        private bool Peek(TokenType tokenType, out Token token)
        {
            token = lexer.Peek();
            return token.Type == tokenType;
        }

        private bool Peek(TokenType tokenType, object value)
        {
            var token = lexer.Peek();
            return
                (token.Type == tokenType &&
                value.Equals(token.Value));
        }

        private bool Peek(params TokenType[] tokenTypes)
        {
            var type = lexer.Peek().Type;
            foreach (var t in tokenTypes)
                if (t == type) return true;
            return false;
        }

        private bool Peek(ISet<TokenType> tokentypes)
        {
            return tokentypes.Contains(lexer.Peek().Type);
        }

        private void SkipUntil(params TokenType[] tokenTypes)
        {
            while (!Peek(tokenTypes))
            {
                var tok = lexer.Get();
                if (tok.Type == TokenType.EOF)
                    return;
            }
        }
        private Token Expect(TokenType tokenType)
        {
            var t = lexer.Peek();
            if (t.Type != tokenType)
            {
                Debug.Print("Expect failed: {0} {1}", filename, t.LineNumber);
                throw Error(Resources.ErrExpectedTokenButSaw, tokenType, t.Type);
            }
            return lexer.Get();
        }

        private T Expect<T>(Func<T?> parserFn, string msg) where T : class
        {
            T? t = parserFn();
            if (t == null)
            {
                throw Error(Resources.ErrInvalidSyntax, msg);
            }
            return t;
        }

        //decorators: decorator+
        public List<Decorator> decorators()
        {
            var dec = decorator();
            var decs = new List<Decorator> { dec };
            while (Peek(TokenType.AT))
            {
                decs.Add(decorator());
            }
            return decs;
        }

        //decorated: decorators (classdef | funcdef)

        public List<Statement> decorated()
        {
            var decs = decorators();
            Statement d;
            for (;;)
            {
                if (Peek(TokenType.Def))
                {
                    d = funcdef()[0];
                    break;
                }
                else if (Peek(TokenType.Class))
                {
                    d = classdef()[0];
                    break;
                }
                else if (!Peek(TokenType.COMMENT, TokenType.NEWLINE))
                {
                    Error(Resources.ErrExpectedFunctionOrClassDefinition, lexer.Peek());
                }
                //$TODO: keep the comments.
                lexer.Get();
            }
            d.Decorators = decs;
            return new List<Statement> { d };
        }

        // funcdef: 'def' NAME parameters ['->' test] ':' suite
        public List<Statement> funcdef()
        {
            var start = Expect(TokenType.Def).Start;
            var token = Expect(TokenType.ID);
            var fnName = new Identifier((string)token.Value!, filename, token.Start, token.End);
            Debug.Print("  Parsing {0}", fnName.Name);
            List<Parameter> parms = parameters();
            Exp? annotation = null;
            if (PeekAndDiscard(TokenType.LARROW))
            {
                annotation = test();
            }
            Expect(TokenType.COLON);
            var s = suite();
            var vararg = parms.Where(p => p.IsVarArg).SingleOrDefault();
            var kwarg = parms.Where(p => p.IsKeyArg).SingleOrDefault();
            var fndef = new FunctionDef(
                fnName,
                parms,
                vararg?.Id,
                kwarg?.Id,
                annotation,
                s,
                filename, start, s.End);
            return new List<Statement> { fndef };
        }

        // parameters: '(' [typedargslist] ')'

        public List<Parameter> parameters()
        {
            Expect(TokenType.LPAREN);
            List<Parameter> args;
            if (Peek(TokenType.RPAREN))
            {
                args = new List<Parameter>();
            }
            else
            {
                args = typedargslist();
            }
            Expect(TokenType.RPAREN);
            return args;
        }


        //typedargslist: 
        // (tfpdef ['=' test] (',' tfpdef ['=' test])* [','  ['*' [tfpdef] (',' tfpdef ['=' test])* [',' '**' tfpdef] | '**' tfpdef]]
        //  |  '*' [tfpdef] (',' tfpdef ['=' test])* [',' '**' tfpdef]
        //  | '**' tfpdef)
        public List<Parameter> typedargslist()
        {
            var args = new List<Parameter>();
            Parameter arg;
            while (!Peek(TokenType.RPAREN))
            {
                switch (lexer.Peek().Type)
                {
                case TokenType.OP_STAR:
                    lexer.Get();
                    if (Peek(TokenType.ID))
                    {
                        arg = fpdef();
                        arg.IsVarArg = true;
                        args.Add(arg);
                    }
                    if (PeekAndDiscard(TokenType.COMMA))
                    {
                        if (PeekAndDiscard(TokenType.OP_STARSTAR))
                        {
                            arg = fpdef();
                            arg.IsKeyArg = true;
                            args.Add(arg);
                            return args;
                        }
                        arg = fpdef();
                        args.Add(arg);
                        if (PeekAndDiscard(TokenType.EQ))
                            arg.Test = test();
                    }
                    return args;
                case TokenType.OP_STARSTAR:
                    lexer.Get();
                    arg = fpdef();
                    args.Add(arg);
                    return args;
                default:
                    // (tfpdef ['=' test] (',' tfpdef ['=' test])* [','  ['*' [tfpdef] (',' tfpdef ['=' test])* [',' '**' tfpdef] | '**' tfpdef]]
                    arg = fpdef();
                    args.Add(arg);
                    if (PeekAndDiscard(TokenType.EQ))
                        arg.Test = test();
                    while (PeekAndDiscard(TokenType.COMMA))
                    {
                        if (PeekAndDiscard(TokenType.COMMENT))
                        {
                            //$TODO: arg-specific comment?
                        }
                        if (Peek(TokenType.RPAREN))
                            break;      // Skip trailing comma
                        if (PeekAndDiscard(TokenType.OP_STARSTAR))
                        {
                            arg = fpdef();
                            arg.IsKeyArg = true;
                            args.Add(arg);
                        }
                        else if (PeekAndDiscard(TokenType.OP_STAR))
                        {
                            if (!Peek(TokenType.COMMA))
                            {
                                // *args
                            arg = fpdef();
                            }
                            else
                            {
                                arg = new Parameter();
                            }
                            arg.IsVarArg = true;
                            args.Add(arg);
                        }
                        else
                        {
                            arg = fpdef();
                            args.Add(arg);
                            if (PeekAndDiscard(TokenType.EQ))
                                arg.Test = test();
                        }
                    }
                    return args;
                }
            }
            throw Unexpected();
        }

        public List<Parameter?> typedarglist()
        {
            List<Parameter?> args = new List<Parameter?>();
            while (Peek(TokenType.ID))
            {
                Parameter t = fpdef();
                if (PeekAndDiscard(TokenType.EQ))
                {
                    t.Test = test();
                }
                args.Add(t);
                if (!PeekAndDiscard(TokenType.COMMA))
                    return args;
            }
            while (PeekAndDiscard(TokenType.OP_STAR))
            {
                Parameter? t = null;
                if (Peek(TokenType.ID))
                    t = fpdef();
                args.Add(t);
                if (!PeekAndDiscard(TokenType.COMMA))
                    return args;
            }
            if (PeekAndDiscard(TokenType.OP_STARSTAR))
            {
                args.Add(fpdef());
            }
            return args;
        }

        // fpdef: NAME | '(' fplist ')'
        // tfpdef: NAME [':' test]
        Parameter fpdef()
        {
            if (PeekAndDiscard(TokenType.LPAREN))
            {
                var fpl = fplist();
                Expect(TokenType.RPAREN);
                return new Parameter
                {
                    tuple = fpl
                };
            }
            string? eolComment = null;
            if (Peek(TokenType.COMMENT))
            {
                eolComment = (string)Expect(TokenType.COMMENT).Value!;
            }
            var name = id();
            Exp? a = null;
            if (PeekAndDiscard(TokenType.COLON))
                a = test();
            return new Parameter
            {
                Id = name,
                Annotation = a,
                Comment = eolComment,
            };
        }

        // fplist: fpdef (',' fpdef)* [',']
        List<Parameter> fplist()
        {
            var fp = fpdef();
            var p = new List<Parameter> { fp };
            if (PeekAndDiscard(TokenType.COMMA))
            {
                while (!Peek(TokenType.EOF, TokenType.RPAREN))
                {
                    p.Add(fpdef());
                    if (!PeekAndDiscard(TokenType.COMMA))
                        return p;
                }
            }
            return p;
        }

        //varargslist: 
        //        (vfpdef ['=' test] (',' vfpdef ['=' test])* [',' ['*' [vfpdef] (',' vfpdef ['=' test])* [',' '**' vfpdef] | '**' vfpdef]]
        //        |  '*' [vfpdef] (',' vfpdef ['=' test])* [',' '**' vfpdef] 
        //        | '**' vfpdef)
        public List<VarArg> varargslist()
        {
            var args = new List<VarArg>();
                    if (PeekAndDiscard(TokenType.LPAREN))
                    {
                args = varargslist();
                        Expect(TokenType.RPAREN);
                return args;
                    }
            if (Peek(TokenType.ID))
                    {
                var vfp = vfpdef_init();
                args.Add(vfp);
                while (PeekAndDiscard(TokenType.COMMA) && Peek(TokenType.ID))
                        {
                    vfp = vfpdef_init();
                    args.Add(vfp);
                        }
                    }
            if (PeekAndDiscard(TokenType.OP_STAR))
            {
                args.Add(VarArg.Indexed(vfpdef()));
                while (PeekAndDiscard(TokenType.COMMA) && Peek(TokenType.ID))
                {
                    var vfp = vfpdef_init();
                    args.Add(vfp);
                }
            }
            if (PeekAndDiscard(TokenType.OP_STARSTAR)) {
                args.Add(VarArg.Keyword(vfpdef()));
            }
            return args;
        }

        public VarArg vfpdef_init()
        {
            var id = vfpdef();
            Exp? init = null;
            if (PeekAndDiscard(TokenType.EQ))
            {
                init = test();
            }
            return new VarArg(id, init);
        }

        //vfpdef: NAME
        public Identifier vfpdef()
        {
            var token = Expect(TokenType.ID);
            return new Identifier((string)token.Value!, filename, token.Start, token.End);
        }

        private readonly static HashSet<TokenType> compoundStatement_first = new HashSet<TokenType>() {
            TokenType.If, TokenType.While, TokenType.For, TokenType.Try, TokenType.With,
            TokenType.Def, TokenType.Class, TokenType.AT, TokenType.Async
        };

        private readonly static HashSet<TokenType> augassign_set = new HashSet<TokenType>() {
            TokenType.ADDEQ, TokenType.SUBEQ, TokenType.MULEQ, TokenType.DIVEQ, TokenType.MODEQ,
            TokenType.ANDEQ, TokenType.OREQ, TokenType.XOREQ, TokenType.SHLEQ, TokenType.SHREQ,
            TokenType.EXPEQ, TokenType.IDIVEQ, TokenType.ATEQ
        };

        private readonly static HashSet<TokenType> stmt_follow = new HashSet<TokenType>() {
            TokenType.SEMI, TokenType.NEWLINE, TokenType.COMMENT, TokenType.EOF
        };

        private readonly static HashSet<TokenType> trailer_first = new HashSet<TokenType> {
            TokenType.LPAREN, TokenType.LBRACKET, TokenType.DOT
        };


        // stmt: simple_stmt | compound_stmt
        public List<Statement> stmt()
        {
            try
            {
            if (Peek(compoundStatement_first))
            {
                return compound_stmt();
            }
            else
            {
                return simple_stmt();
            }
        }
            catch (Exception ex)
            {
                if (!catchExceptions)
                    throw;
                logger.Error(ex, $"({this.filename},{lexer.LineNumber}): parser error. Please report on https://github.com/uxmal/pytocs");
                SkipUntil(TokenType.COMMENT, TokenType.Def, TokenType.Class);
                return new List<Statement> {
                    new CommentStatement(this.filename, 0, 0)
                    {
                        Comment = "<parser-error>",
                    }
                };
            }
        }

        //simple_stmt: small_stmt (';' small_stmt)* [';'] NEWLINE
        public List<Statement> simple_stmt()
        {
            List<Statement> stmts = new List<Statement>();
            var s = small_stmt();
            if (s != null)
            {
                stmts.Add(s);
            }
            while (PeekAndDiscard(TokenType.SEMI))
            {
                if (Peek(TokenType.EOF))
                    break;
                if (PeekAndDiscard(TokenType.NEWLINE))
                {
                    return new List<Statement> {
                        new SuiteStatement(stmts, filename, stmts[0].Start, stmts.Last().End)
                    };
                }
                s = small_stmt();
                if (s != null)
                {
                    stmts.Add(s);
                }
            }
            string? comment = null;
            if (!Peek(TokenType.EOF))
            {
                if (Peek(TokenType.COMMENT))
                {
                    comment = (string)Expect(TokenType.COMMENT).Value!;
                }
                Expect(TokenType.NEWLINE);
            }
            if (stmts.Count == 0)
            {
                return new List<Statement>();
            }
            else
            {
                return new List<Statement>
                {
                    new SuiteStatement(stmts, filename, stmts[0].Start, stmts.Last().End) { Comment = comment }
                };
            }
        }

        //small_stmt: (expr_stmt | del_stmt | pass_stmt | flow_stmt |
        //             import_stmt | global_stmt | nonlocal_stmt | assert_stmt)
        public Statement? small_stmt()
        {
            switch (lexer.Peek().Type)
            {
            case TokenType.Del: return del_stmt();
            case TokenType.Pass: return pass_stmt();
            case TokenType.Break: return break_stmt();
            case TokenType.Continue: return continue_stmt();
            case TokenType.Return: return return_stmt();
            case TokenType.Raise: return raise_stmt();
            case TokenType.Yield: return yield_stmt();
            case TokenType.Import: return import_stmt();
            case TokenType.From: return import_stmt();
            case TokenType.Global: return global_stmt();
            case TokenType.Nonlocal: return nonlocal_stmt();
            case TokenType.Assert: return assert_stmt();
            case TokenType.Exec: return exec_stmt();
            case TokenType.COMMENT: return comment_stmt();
            case TokenType.INDENT:
                Expect(TokenType.INDENT);
                if (PeekAndDiscard(TokenType.COMMENT, out var c))
                {
                    return new CommentStatement(filename, c.Start, c.End) { Comment = (string)c.Value! };
                }
                else
                {
                    return null;
                }
            case TokenType.DEDENT:
                Expect(TokenType.DEDENT);
                if (PeekAndDiscard(TokenType.COMMENT, out var cc))
                {
                    return new CommentStatement(filename, cc.Start, cc.End) { Comment = (string)cc.Value! };
                }
                else
                {
                    return null;
                }
            default: return expr_stmt();
            }
        }

        //expr_stmt: testlist_star_expr (annasign | augassign (yield_expr|testlist)) |
        //                     ('=' (yield_expr|testlist_star_expr))*)
        public Statement expr_stmt()
        {
            // Hack to deal with print statement from python 2.*
            if (Peek(TokenType.ID, "print"))
            {
                return print_stmt();
            }
            var lhs = testlist_star_expr();
            if (Peek(TokenType.COLON))
            {
                var (a, i) = annasign();
                //$TODO: distinguish declarations from assignments.
                var ass = new AssignExp(lhs, a, Op.Assign, i!, filename, lhs.Start, (i ?? a).End);
                lhs = ass;
            }
            if (Peek(augassign_set))
            {
                var op = augassign();
                Exp e2;
                if (Peek(TokenType.Yield))
                    e2 = yield_expr();
                else
                    e2 = testlist();
                lhs = new AssignExp(lhs, op, e2, filename, lhs.Start, e2.End);
            }
            else
            {
                var rhsStack = new Stack<Exp>();
                while (PeekAndDiscard(TokenType.EQ))
                {
                    Exp r;
                    if (Peek(TokenType.Yield))
                        r = yield_expr();
                    else
                        r = testlist_star_expr();
                    rhsStack.Push(r);
                }
                if (rhsStack.Count > 0)
                {
                    Exp e = rhsStack.Pop();
                    while (rhsStack.Count > 0)
                    {
                        var ee = rhsStack.Pop();
                        e = new AssignExp(ee, Op.Assign, e, filename, ee.Start, e.End);
            }
                    lhs = new AssignExp(lhs, Op.Assign, e, filename, lhs.Start, e.End);
                }
            }
            return new ExpStatement(lhs, filename, lhs.Start, lhs.End);
        }

        //print_stmt: 'print' ( [ test (',' test)* [','] ] |
        //                      '>>' test [ (',' test)+ [','] ] )
        public Statement print_stmt()
        {
            var args = new List<Argument>();
            Exp? outputStream = null;    // default to stdout.
            bool trailing_comma = false;

            Identifier printId = id();
            int posEnd = printId.End;
            if (!Peek(TokenType.NEWLINE))
            {
                if (PeekAndDiscard(TokenType.OP_SHR))
                {
                    outputStream = ExpectTest();
                    if (PeekAndDiscard(TokenType.COMMA))
                    {
                        var e = ExpectTest();
                        args.Add(new Argument(null, e, filename, e.Start, e.End));
                        while (PeekAndDiscard(TokenType.COMMA))
                        {
                            var d = ExpectTest();
                            args.Add(new Argument(null, d, filename, e.Start, d.End));
                        }
                        posEnd = args.Last().End;
                    }
                }
                else if (Peek(TokenType.LPAREN))
                {
                    var tok = lexer.Get();
                    args = arglist(printId, tok.Start).Args;
                    Expect(TokenType.RPAREN);
                    if (PeekAndDiscard(TokenType.COMMA))
                    {
                        trailing_comma = true;
                }
                }
                else
                {
                    for (; ; )
                    {
                        var tok = lexer.Peek();
                        if (tok.Type == TokenType.EOF ||
                            tok.Type == TokenType.NEWLINE ||
                            tok.Type == TokenType.SEMI)
                            break;
                        var a = ExpectTest();
                        args.Add(new Argument(null, a, filename, a.Start, a.End));
                        posEnd = a.End;
                        if (!PeekAndDiscard(TokenType.COMMA))
                            break;
                        if (Peek(stmt_follow))
                        {
                            trailing_comma = true;
                            break;
                        }
                    }
                }
            }
            return new PrintStatement(outputStream, args, trailing_comma, filename, printId.Start, posEnd);
        }

        //testlist_star_expr: (test|star_expr) (',' (test|star_expr))* [',']
        public Exp testlist_star_expr()
        {
            List<Exp> exprs = new();
            bool forceSingletonTuple = false;
            for (; ; )
            {
                Exp? e;
                if (Peek(TokenType.OP_STAR))
                    e = star_expr();
                else
                    e = test();
                if (e is null)
                    break;
                exprs.Add(e);
                if (!PeekAndDiscard(TokenType.COMMA))
                    break;
                forceSingletonTuple = true;
                if (Peek(stmt_follow))
                    break;
            }
            return exprs.Count == 1 && !forceSingletonTuple ? exprs[0] : new ExpList(exprs, filename, 0, 0);
        }

        // annassign: ':' test ['=' test]
        public (Exp, Exp?) annasign()
        {
            Expect(TokenType.COLON);
            Exp? annotation = test();
            if (annotation is null)
                throw Unexpected();
            Exp? initializer = null;
            if (PeekAndDiscard(TokenType.EQ))
            {
                initializer = test();
            }
            return (annotation, initializer);
        }

        //augassign: ('+=' | '-=' | '*=' | '/=' | '%=' | '&=' | '|=' | '^=' |
        //            '<<=' | '>>=' | '**=' | '//=')
        public Op augassign()
        {
            switch (lexer.Get().Type)
            {
            case TokenType.ADDEQ: return Op.AugAdd;
            case TokenType.SUBEQ: return Op.AugSub;
            case TokenType.MULEQ: return Op.AugMul;
            case TokenType.DIVEQ: return Op.AugDiv;
            case TokenType.MODEQ: return Op.AugMod;
            case TokenType.ANDEQ: return Op.AugAnd;
            case TokenType.OREQ: return Op.AugOr;
            case TokenType.XOREQ: return Op.AugXor;
            case TokenType.SHLEQ: return Op.AugShl;
            case TokenType.SHREQ: return Op.AugShr;
            case TokenType.EXPEQ: return Op.AugExp;
            case TokenType.IDIVEQ: return Op.AugIDiv;
            case TokenType.ATEQ: return Op.AugMatMul;
            default: throw Unexpected();
            }
        }

        // For normal assignments, additional restrictions enforced by the interpreter
        // del_stmt: 'del' exprlist
        public Statement del_stmt()
        {
            var posStart = Expect(TokenType.Del).Start;
            var e = exprlist();
            return new DelStatement(e, filename, posStart, e.End);
        }

        //pass_stmt: 'pass'
        public Statement pass_stmt()
        {
            var token = Expect(TokenType.Pass);
            return new PassStatement(filename, token.Start, token.End);
        }

        //flow_stmt: break_stmt | continue_stmt | return_stmt | raise_stmt | yield_stmt
        //break_stmt: 'break'
        public Statement break_stmt()
        {
            var token = Expect(TokenType.Break);
            return new BreakStatement(filename, token.Start, token.End);
        }

        //continue_stmt: 'continue'
        public Statement continue_stmt()
        {
            var token = Expect(TokenType.Continue);
            return new ContinueStatement(filename, token.Start, token.End);
        }

        //return_stmt: 'return' [testlist_star_expr]
        public Statement return_stmt()
        {
            var token = Expect(TokenType.Return);
            var posStart = token.Start;
            var posEnd = token.End;
            Exp? e = null;
            if (!Peek(stmt_follow))
            {
                e = testlist_star_expr();
                if (e != null)
                {
                    posEnd = e.End;
                }
            }
            return new ReturnStatement(e, filename, posStart, posEnd);
        }

        //yield_stmt: yield_expr
        public Statement yield_stmt()
        {
            var e =  yield_expr();
            return new YieldStatement(e, filename, e.Start, e.End);
        }

        //raise_stmt: 'raise' [test ['from' test]]
        //raise_stmt: 'raise' [test [',' test [',' Test]]]
        public Statement raise_stmt()
        {
            Exp? exToRaise = null;
            Exp? exOriginal = null;
            var token = Expect(TokenType.Raise);
            var posStart = token.Start;
            var posEnd = token.End;
            if (!Peek(stmt_follow))
            {
                exToRaise = test();
                if (exToRaise != null)
                {
                    posEnd = exToRaise.End;
                    if (PeekAndDiscard(TokenType.From))
                    {
                        exOriginal = ExpectTest();
                        posEnd = exOriginal.End;
                    }
                    else if (PeekAndDiscard(TokenType.COMMA))
                    {
                        Exp ex2 = ExpectTest();
                        Exp? ex3 = new NoneExp(filename, ex2.End, ex2.End);
                        if (PeekAndDiscard(TokenType.COMMA))
                        {
                            ex3 = ExpectTest();
                        }
                        exOriginal = new PyTuple(new List<Exp> { ex2, ex3! }, filename, posStart, (ex3 ?? ex2).End);
                        posEnd = exOriginal.End;
                    }
                }
            }
            return new RaiseStatement(exToRaise, exOriginal, filename, posStart, posEnd);
        }

        //import_stmt: import_name | import_from
        public Statement import_stmt()
        {
            if (Peek(TokenType.Import))
                return import_name();
            else
                return import_from();
        }

        //import_name: 'import' dotted_as_names
        public Statement import_name()
        {
            var posStart = Expect(TokenType.Import).Start;
            var names = dotted_as_names();
            return new ImportStatement(names, filename, posStart, names.Last().End);
        }

        // note below: the ('.' | '...') is necessary because '...' is tokenized as ELLIPSIS
        //import_from: ('from' (('.' | '...')* dotted_name | ('.' | '...')+)
        //              'import' ('*' | '(' import_as_names ')' | import_as_names))
        public Statement import_from()
        {
            var posStart = Expect(TokenType.From).Start;
            DottedName? name = null;
            if (Peek(TokenType.DOT, TokenType.ELLIPSIS))
            {
                lexer.Get();
                while (Peek(TokenType.DOT, TokenType.ELLIPSIS))
                {
                    lexer.Get();
                }
                if (Peek(TokenType.ID))
                    name = dotted_name();
            }
            else
            {
                name = dotted_name();
            }
            Expect(TokenType.Import);
            int posEnd;
            List<AliasedName>? aliasNames;
            if (PeekAndDiscard(TokenType.OP_STAR, out var token))
            {
                aliasNames = new List<AliasedName>();
                posEnd = token.End;
            }
            else if (PeekAndDiscard(TokenType.LPAREN))
            {
                aliasNames = import_as_names();
                posEnd = Expect(TokenType.RPAREN).End;
            }
            else
            {
                aliasNames = import_as_names();
                posEnd = aliasNames.Last().End;
            }
            return new FromStatement(name, aliasNames, filename, posStart, posEnd);
        }

        //import_as_name: NAME ['as' NAME]
        public AliasedName import_as_name()
        {
            var orig = id();
            if (PeekAndDiscard(TokenType.As))
            {
                var alias = id();
            return new AliasedName(orig,  alias, filename, orig.Start, alias.End);
        }
            else
            {
                return new AliasedName(orig, null, filename, orig.Start, orig.End);
            }
        }

        //dotted_as_name: dotted_name ['as' NAME]
        public AliasedName dotted_as_name()
        {
            var orig = dotted_name();
            if (PeekAndDiscard(TokenType.As))
            {
                var alias = id();
                return new AliasedName(orig, alias, filename, orig.Start, alias.End);
            }
            else
            {
                return new AliasedName(orig, null, filename, orig.Start, orig.End);
            }
        }

        //import_as_names: import_as_name (',' import_as_name)* [',']
        public List<AliasedName> import_as_names()
        {
            var aliases = new List<AliasedName>();
            CollectComments();  //$TODO: we should be keeping these somehow
            aliases.Add(import_as_name());
            while (PeekAndDiscard(TokenType.COMMA))
            {
                CollectComments();
                if (!Peek(TokenType.ID))
                    break;
                aliases.Add(import_as_name());
            }
            return aliases;
        }
        //dotted_as_names: dotted_as_name (',' dotted_as_name)*
        public List<AliasedName> dotted_as_names()
        {
            var alias = dotted_as_name();
            var aliases = new List<AliasedName> { alias };
            while (PeekAndDiscard(TokenType.COMMA))
            {
                aliases.Add(dotted_as_name());
            }
            return aliases;
        }

        //dotted_name: NAME ('.' NAME)*
        public DottedName dotted_name()
        {
            var segs = new List<Identifier>();
            var name = id();
            var posStart = name.Start;
            var posEnd = name.End;
            segs.Add(name);
            while (PeekAndDiscard(TokenType.DOT))
            {
                name = id();
                posEnd = name.End;
                segs.Add(name);
            }
            return new DottedName(segs, filename, posStart, posEnd);
        }

        //global_stmt: 'global' NAME (',' NAME)*
        public Statement global_stmt()
        {
            var posStart = Expect(TokenType.Global).Start;
            var names = new List<Identifier>();
            var name = id();
            var posEnd = name.End;
            names.Add(name);
            while (PeekAndDiscard(TokenType.COMMA))
            {
                name = id();
                posEnd = name.End;
                names.Add(name);
            }
            return new GlobalStatement(names, filename, posStart, posEnd);
        }

        //nonlocal_stmt: 'nonlocal' NAME (',' NAME)*
        public Statement nonlocal_stmt()
        {
            var posStart = Expect(TokenType.Nonlocal).Start;
            var names = new List<Identifier>();
            var name = id();
            var posEnd = name.End;
            names.Add(name);
            while (PeekAndDiscard(TokenType.COMMA))
            {
                name = id();
                posEnd = name.End;
                names.Add(name);
            }
            return new NonlocalStatement(names, filename, posStart, posEnd);
        }

        //assert_stmt: 'assert' test [',' test]
        public Statement assert_stmt()
        {
            var posStart = Expect(TokenType.Assert).Start;
            var test = ExpectTest();
            var tests = new List<Exp> { test };
            while (PeekAndDiscard(TokenType.COMMA))
            {
                tests.Add(ExpectTest());
            }
            return new AssertStatement(tests, filename, posStart, tests.Last().End);
        }

        //exec_stmt: 'exec' expr ['in' test [',' test]]
        public ExecStatement exec_stmt()
        {
            var posStart = Expect(TokenType.Exec).Start;
            Exp? e = expr();
            if (e is null) throw Unexpected();
            var posEnd = e.End;
            Exp? g = null;
            Exp? l = null;
            if (PeekAndDiscard(TokenType.In))
            {
                g = ExpectTest();
                posEnd = g.End;
                if (PeekAndDiscard(TokenType.COMMA))
                {
                    l = ExpectTest();
                    if (l != null)
                    posEnd = l.End;
                }
            }
            return new ExecStatement(e, g, l, filename, posStart, posEnd);
        }

        public CommentStatement comment_stmt()
        {
            var token = Expect(TokenType.COMMENT);
            return new CommentStatement(filename, token.Start, token.End)
            {
                Comment = (string)token.Value!
            };
        }

        //compound_stmt: if_stmt | while_stmt | for_stmt | try_stmt | with_stmt | funcdef | classdef | decorated
        public List<Statement> compound_stmt()
        {
            switch (lexer.Peek().Type)
            {
            case TokenType.If: return if_stmt();
            case TokenType.While: return while_stmt();
            case TokenType.For: return for_stmt();
            case TokenType.Try: return try_stmt();
            case TokenType.With: return with_stmt();
            case TokenType.Def: return funcdef();
            case TokenType.Class: return classdef();
            case TokenType.AT: return decorated();
            case TokenType.Async: return async_stmt();
            default: throw Unexpected();
            }
        }

        // async_stmt: 'async' (funcdef | with_stmt | for_stmt)
        public List<Statement> async_stmt()
        {
            var posStart = Expect(TokenType.Async).Start;
            List<Statement> stm;
            switch (lexer.Peek().Type)
            {
            case TokenType.Def: stm = funcdef(); break;
            case TokenType.With: stm = with_stmt(); break;
            case TokenType.For: stm = for_stmt(); break;
            default: throw Unexpected();
            }
            var filename = stm.Last().Filename;
            var posEnd = stm.Last().End;
            var asynk = new AsyncStatement(stm[0], filename, posStart, posEnd);
            return new List<Statement> { asynk };
        }

        //if_stmt: 'if' test ':' suite ('elif' test ':' suite)* ['else' ':' suite]
        public List<Statement> if_stmt()
        {
            var posStart = Expect(TokenType.If).Start;
            Exp t = ExpectTest();
            Expect(TokenType.COLON);
            var ts = suite();
            var stack = new Stack<Tuple<int, Exp?, SuiteStatement>>();
            stack.Push(Tuple.Create(posStart, (Exp?)t, ts));
            var comments = CollectComments();
            while (PeekAndDiscard(TokenType.Elif, out var token))
            {
                t = ExpectTest();
                Expect(TokenType.COLON);
                ts = suite();
                ts.Statements.InsertRange(0, comments);
                stack.Push(Tuple.Create(token.Start, (Exp?)t, ts));
                comments = CollectComments();
            }
            if (PeekAndDiscard(TokenType.Else, out var token2))
            {
                Expect(TokenType.COLON);
                ts = suite();
                ts.Statements.InsertRange(0, comments);
                comments.Clear();
                stack.Push(new Tuple<int, Exp?, SuiteStatement>(token2.Start, null, ts));
            }

            SuiteStatement? es = null;
            if (stack.Peek().Item2 == null)
            {
                es = stack.Pop().Item3;
            }
            IfStatement ifStmt;
            do
            {
                var item = stack.Pop();
                ifStmt = new IfStatement(
                    item.Item2!,
                    item.Item3,
                    es,
                    filename, item.Item1, item.Item3.End);
                es = new SuiteStatement(new List<Statement> { ifStmt }, filename, ifStmt.Start, ifStmt.End);
            } while (stack.Count > 0);
            var result = new List<Statement> { ifStmt };
            result.AddRange(comments);
            return result;
        }

        //while_stmt: 'while' test ':' suite ['else' ':' suite]
        public List<Statement> while_stmt()
        {
            var posStart = Expect(TokenType.While).Start;
            var t = test();
            if (t is null) throw Unexpected();
            Expect(TokenType.COLON);
            var s = suite();
            var posEnd = s.End;
            SuiteStatement? es = null;
            if (PeekAndDiscard(TokenType.Else))
            {
                Expect(TokenType.COLON);
                es = suite();
                posEnd = es.End;
            }
            var w = new WhileStatement(t, s, es, filename, posStart, posEnd);
            return new List<Statement> { w };
        }

        //for_stmt: 'for' exprlist 'in' testlist ':' suite ['else' ':' suite]
        public List<Statement> for_stmt()
        {
            var posStart = Expect(TokenType.For).Start;
            var ell = exprlist();
            Expect(TokenType.In);
            var tl = testlist();
            Expect(TokenType.COLON);
            var body = suite();
            var posEnd = body.End;
            SuiteStatement? es = null;
            if (PeekAndDiscard(TokenType.Else))
            {
                Expect(TokenType.COLON);
                es = suite();
                posEnd = es.End;
            }
            var f = new ForStatement(ell, tl, body, es, filename, posStart, posEnd);
            return new List<Statement> { f };
        }

        //try_stmt: ('try' ':' suite
        //           ((except_clause ':' suite)+
        //            ['else' ':' suite]
        //            ['finally' ':' suite] |
        //           'finally' ':' suite))
        public List<Statement> try_stmt()
        {
            var posStart = Expect(TokenType.Try).Start;
            Expect(TokenType.COLON);
            string? c = null;
            if (Peek(TokenType.COMMENT))
            {
                c = comment_stmt().Comment;
            }
            var body = suite();
            body.Comment = c;
            int posEnd = body.End;
            var exHandlers = new List<ExceptHandler>();
            Statement? elseHandler = null;
            SuiteStatement? finallyHandler = null;

            // Collect comments right before the except/finally.
            var comments = CollectComments();
            while (Peek(TokenType.Except, out var token))
            {
                var ec = except_clause();
                Expect(TokenType.COLON);
                var handler = suite();
                handler.Statements.InsertRange(0, comments);
                posEnd = handler.End;
                exHandlers.Add(new ExceptHandler(ec.Exp, ec.Alias, handler, filename, token.Start, posEnd));
                comments = CollectComments();
                if (PeekAndDiscard(TokenType.Else))
                {
                    handler.Statements.AddRange(comments);
                    Expect(TokenType.COLON);
                    elseHandler = suite();
                    posEnd = elseHandler.End;
                }
            }
            if (PeekAndDiscard(TokenType.Finally))
            {
                Expect(TokenType.COLON);
                finallyHandler = suite();
                finallyHandler.Statements.InsertRange(0, comments);
                posEnd = finallyHandler.End;
            }
            if (exHandlers.Count == 0 && finallyHandler == null)
                throw Error(Resources.ErrExpectedAtLeastOneExceptClause);
            var t = new TryStatement(body, exHandlers, elseHandler, finallyHandler, filename, posStart, posEnd);
            return new List<Statement> { t };
        }

        private List<CommentStatement> CollectComments()
        {
            var cmts = new List<CommentStatement>();
            while (Peek(TokenType.COMMENT, out var token))
            {
                Expect(TokenType.COMMENT);
                cmts.Add(new CommentStatement(filename, token.Start, token.End)
                {
                    Comment = (string)token.Value!
                });
                PeekAndDiscard(TokenType.NEWLINE);
            }
            return cmts;

        }

        // with_stmt: 'with' with_item (',' with_item)*  ':' suite
        public List<Statement> with_stmt()
        {
            var posStart = Expect(TokenType.With).Start;
            var ws = new List<WithItem>();
            var w = with_item();
            if (w != null)
            {
                ws.Add(w);
                while (PeekAndDiscard(TokenType.COMMA))
                {
                    w = with_item();
                    if (w == null)
                        break;
                    ws.Add(w);
                }
                Expect(TokenType.COLON);
                var s = suite();
                return new List<Statement> {
                    new WithStatement(ws, s, filename, posStart, s.End)
                };
            }
            else
                return new List<Statement>();
        }

        //with_item: test ['as' expr]
        public WithItem? with_item()
        {
            var t = test();
            if (t is null)
                throw Unexpected();
            Exp? e = null;
            if (PeekAndDiscard(TokenType.As))
                e = expr();
            return new WithItem(t, e, filename, t.Start, (e ?? t).End);
        }

        // NB compile.c makes sure that the default except clause is last
        //except_clause: 'except' [test ['as' NAME]]
        public AliasedExp except_clause()
        {
            var token = Expect(TokenType.Except);
            var posStart = token.Start;
            var posEnd = token.End;
            Exp? t = null;
            Identifier? alias = null;
            if (!Peek(TokenType.COLON))
            {
                t = ExpectTest();
                posEnd = t.End;
                if (PeekAndDiscard(TokenType.As))
                {
                    alias = id();
                    posEnd = alias.End;
                }
                else if (PeekAndDiscard(TokenType.COMMA))
                {
                    alias = id();
                    posEnd = token.End;
                }
            }
            return new AliasedExp(t, alias, filename, posStart, posEnd);
        }

        //suite: simple_stmt | NEWLINE INDENT stmt+ DEDENT
        public SuiteStatement suite()
        {
            List<Statement> stmts = new List<Statement>();
            if (Peek(TokenType.COMMENT))
            {
                stmts.Add(comment_stmt());
            }
            if (PeekAndDiscard(TokenType.NEWLINE))
            {
                while (!Peek(TokenType.INDENT))
                {
                    var token = Expect(TokenType.COMMENT);
                    stmts.Add(new CommentStatement(filename, token.Start, token.End) { Comment = (string)token.Value! });
                    Expect(TokenType.NEWLINE);
                }
                Expect(TokenType.INDENT);
                while (!Peek(TokenType.EOF) && !PeekAndDiscard(TokenType.DEDENT))
                {
                    stmts.AddRange(stmt());
                }
            }
            else
            {
                var stmt = simple_stmt();
                stmts.AddRange(stmt);
            }
            return new SuiteStatement(stmts, filename, stmts[0].Start, stmts.Last().End);
        }

        //test: or_test ['if' or_test 'else' test] | lambdef
        public Exp? test()
        {
            while (PeekAndDiscard(TokenType.COMMENT))
                ;
            if (Peek(TokenType.Lambda))
                return lambdef();
            var consequent = or_test();
            if (consequent == null)
                return consequent;
            if (PeekAndDiscard(TokenType.If))
            {
                var condition = or_test();
                if (condition == null)
                    return null;
                Expect(TokenType.Else);
                var alternative = test();
                if (alternative is null) throw Unexpected();
                return new TestExp(consequent, condition, alternative, filename, consequent.Start, alternative.End);
            }
            if (consequent is Identifier id)
            {
                if (PeekAndDiscard(TokenType.COLONEQ))
                {
                    var e = ExpectTest();
                    return new AssignmentExp(id, e, filename, id.Start, e.End);
                }
            }
            return consequent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Exp ExpectTest()
        {
            var e = test();
            if (e is null)
                throw Unexpected();
            return e;
        }

        //test_nocond: or_test | lambdef_nocond
        public Exp? test_nocond()
        {
            if (Peek(TokenType.Lambda))
                return lambdef_nocond();
            return or_test();
        }

        //lambdef: 'lambda' [varargslist] ':' test
        public Lambda? lambdef()
        {
            var posStart = Expect(TokenType.Lambda).Start;
            var argsList = new List<VarArg>();
            if (!Peek(TokenType.COLON))
                argsList = varargslist();
            Expect(TokenType.COLON);
            var t = ExpectTest();
            return new Lambda(argsList, t, filename, posStart, t.End);
        }

        //lambdef_nocond: 'lambda' [varargslist] ':' test_nocond
        public Lambda? lambdef_nocond()
        {
            var posStart = Expect(TokenType.Lambda).Start;
            var argsList = new List<VarArg>();
            if (!Peek(TokenType.COLON))
                argsList = varargslist();
            Expect(TokenType.COLON);
            var body = test_nocond();
            if (body == null)
                return null;
            return new Lambda(argsList, body, filename, posStart, body.End);
        }

        //or_test: and_test ('or' and_test)*
        public Exp? or_test()
        {
            Exp? e = and_test();
            if (e is null)
                return e;
            while (PeekAndDiscard(TokenType.Or))
            {
                var r = and_test();
                if (r == null)
                    throw Unexpected();
                e = new BinExp(Op.LogOr, e, r, filename, e.Start, r.End);
            }
            return e;
        }

        //and_test: not_test ('and' not_test)*
        public Exp? and_test()
        {
            Exp? e = not_test();
            if (e == null)
                return e;
            for (; ; )
            {
                if (Peek(TokenType.COMMENT))
                {
                    e.Comment = (string)Expect(TokenType.COMMENT).Value!;
                    continue;
                }
                if (!PeekAndDiscard(TokenType.And))
                    break;

                var r = not_test();
                if (r == null)
                    throw Unexpected();
                e = new BinExp(Op.LogAnd, e, r, filename, e.Start, r.End);
            }
            return e;
        }

        //not_test: 'not' not_test | comparison
        public Exp? not_test()
        {
            if (Peek(TokenType.COMMENT))
            {
                //$TODO: discarding a comment here.
                Expect(TokenType.COMMENT);
            }
            if (PeekAndDiscard(TokenType.Not, out var token))
            {
                var test = not_test();
                if (test is null) throw Unexpected();
                return new UnaryExp(Op.Not, test, filename, token.Start, test.End);
            }
            else
            {
                return comparison();
            }
        }

        //comparison: expr (comp_op expr)*
        public Exp? comparison()
        {
            var e = expr();
            if (e is null)
                return e;
            int posStart = e.Start;
            Exp? inner;
            for (; ; )
            {
                Op op;
                //'<'|'>'|'=='|'>='|'<='|'<>'|'!='|'in'|'not' 'in'|'is'|'is' 'not'
                switch (lexer.Peek().Type)
                {
                case TokenType.OP_LT: op = Op.Lt; break;
                case TokenType.OP_LE: op = Op.Le; break;
                case TokenType.OP_GE: op = Op.Ge; break;
                case TokenType.OP_GT: op = Op.Gt; break;
                case TokenType.OP_EQ: op = Op.Eq; break;
                case TokenType.OP_NE: op = Op.Ne; break;
                case TokenType.In: op = Op.In; break;
                case TokenType.Not:
                    Expect(TokenType.Not);
                    Expect(TokenType.In);
                    inner = expr();
                    if (inner is null) throw Unexpected();
                    e = new BinExp(Op.NotIn, e, inner, filename, posStart, inner.End);
                    continue;
                case TokenType.Is:
                    Expect(TokenType.Is);
                    op = PeekAndDiscard(TokenType.Not) ? Op.IsNot : Op.Is;
                    inner = expr();
                if (inner is null) throw Unexpected();
                    e = new BinExp(op, e, inner, filename, posStart, inner.End);
                    continue;

                default: return e;
                }
                lexer.Get();
                inner = expr();
                if (inner is null) throw Unexpected();
                e = new BinExp(op, e, inner, filename, posStart, inner.End);
            }
        }

        // <> isn't actually a valid comparison operator in Python. It's here for the
        // sake of a __future__ import described in PEP 401
        //comp_op: '<'|'>'|'=='|'>='|'<='|'<>'|'!='|'in'|'not' 'in'|'is'|'is' 'not'
        //star_expr: '*' expr
        public Exp? star_expr()
        {
            var posStart = Expect(TokenType.OP_STAR).Start;
            var e = expr();
            if (e == null)
                return e;
            return new StarExp(e, filename, posStart, e.End);
        }

        //expr: xor_expr ('|' xor_expr)*
        public Exp? expr()
        {
            var e = xor_expr();
            if (e == null)
                return e;
            while (PeekAndDiscard(TokenType.OP_BAR))
            {
                var r = xor_expr();
                if (r is null)
                    throw Unexpected();
                e = new BinExp(Op.BitOr, e, r, filename, e.Start, r.End);
            }
            return e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Exp ExpectExpr()
        {
            var e = expr();
            if (e is null)
                throw Unexpected();
            return e;
        }

        //xor_expr: and_expr ('^' and_expr)*
        public Exp? xor_expr()
        {
            var e = and_expr();
            if (e is null)
                return null;
            while (PeekAndDiscard(TokenType.OP_CARET))
            {
                var r = and_expr();
                if (r == null)
                    throw Unexpected();
                e = new BinExp(Op.Xor, e, r, filename, e.Start, r.End);
            }
            return e;
        }

        //and_expr: shift_expr ('&' shift_expr)*
        public Exp? and_expr()
        {
            var e = shift_expr();
            if (e == null)
                return null;
            while (PeekAndDiscard(TokenType.OP_AMP))
            {
                var r = shift_expr();
                if (r == null)
                    throw Unexpected();
                e = new BinExp(Op.BitAnd, e, r, filename, e.Start, r.End);
            }
            return e;
        }

        //shift_expr: arith_expr (('<<'|'>>') arith_expr)*
        public Exp? shift_expr()
        {
            var e = arith_expr();
            if (e == null)
                return null;
            for (; ; )
            {
                Op op;
                switch (lexer.Peek().Type)
                {
                case TokenType.OP_SHL: lexer.Get(); op = Op.Shl; break;
                case TokenType.OP_SHR: lexer.Get(); op = Op.Shr; break;
                default: return e;
                }
                var r = arith_expr();
                if (r == null)
                    throw Unexpected();
                e = new BinExp(op, e, r, filename, e.Start, r.End);
            }
        }

        //arith_expr: term (('+'|'-') term)*
        public Exp? arith_expr()
        {
            var e = term();
            if (e is null)
                return null;
            for (; ; )
            {
                Op op;
                switch (lexer.Peek().Type)
                {
                case TokenType.OP_PLUS: op = Op.Add; break;
                case TokenType.OP_MINUS: op = Op.Sub; break;
                default: return e;
                }
                lexer.Get();
                var r = term();
                if (r == null)
                    throw Unexpected();
                e = new BinExp(op, e, r, filename, e.Start, r.End);
            }
        }

        //term: factor (('*'|'/'|'%'|'//'|'@') factor)*
        public Exp? term()
        {
            var e = factor();
            if (e == null)
                return null;
            for (; ; )
            {
                Op op;
                switch (lexer.Peek().Type)
                {
                case TokenType.OP_STAR: op = Op.Mul; break;
                case TokenType.OP_SLASH: op = Op.Div; break;
                case TokenType.OP_SLASHSLASH: op = Op.IDiv; break;
                case TokenType.OP_PERCENT: op = Op.Mod; break;
                case TokenType.AT: op = Op.MatMul; break;
                default: return e;
                }
                lexer.Get();
                var r = factor();
                if (r is null)
                    throw Unexpected();
                e = new BinExp(op, e, r, filename, e.Start, r.End);
            }
        }

        //factor: ('+'|'-'|'~') factor | power
        public Exp? factor()
        {
            Op op;
            int posStart;
            switch (lexer.Peek().Type)
            {
            case TokenType.OP_PLUS:  posStart = lexer.Get().Start; op = Op.Add; break;
            case TokenType.OP_MINUS: posStart = lexer.Get().Start; op = Op.Sub; break;
            case TokenType.OP_TILDE: posStart = lexer.Get().Start; op = Op.Complement; break;
            default: return power();
            }
            var e = factor();
            if (e == null)
                throw Unexpected();
            return new UnaryExp(op, e, filename, posStart, e.End);
        }

        // power: atom_expr ['**' factor]
        public Exp? power()
        {
            var e = atom_expr();
            if (e == null)
                return null;
            if (PeekAndDiscard(TokenType.OP_STARSTAR))
            {
                var r = factor();
                if (r == null)
                    throw Unexpected();
                e = new BinExp(Op.Exp, e, r, filename, e.Start, r.End);
            }
            return e;
        }

        // atom_expr: ['await'] atom trailer*
        public Exp? atom_expr()
        {
            int posStart = -1;
            if (lexer.Peek().Type == TokenType.Await)
            {
                posStart = lexer.Get().Start;
            }
            var e = atom();
            if (e is null)
                return null;
            while (Peek(trailer_first))
            {
                e = trailer(e);
            }
            if (e is null)
                return e;
            if (posStart >= 0)
            {
                return new AwaitExp(e, e.Filename, posStart, e.End);
            }
            else
            {
                return e;
            }
        }


        //atom: ('(' [yield_expr|testlist_comp] ')' |
        //       '[' [testlist_comp] ']' |
        //       '{' [dictorsetmaker] '}' |
        //       NAME | NUMBER | STRING+ | '...' | 'None' | 'True' | 'False')
        public Exp? atom()
        {
            Exp? e;
            Token t;
            while (PeekAndDiscard(TokenType.COMMENT))
                ;
            switch (lexer.Peek().Type)
            {
            case TokenType.LPAREN:
                lexer.Get();
                if (Peek(TokenType.Yield))
                {
                    e = yield_expr();
                }
                else
                {
                    e = testlist_comp(true);
                }
                Expect(TokenType.RPAREN);
                return e;
            case TokenType.LBRACKET:
                var lbr = lexer.Get();
                e = testlist_comp(false);
                var rbr = Expect(TokenType.RBRACKET);
                if (e is CompFor)
                {
                    e = new ListComprehension(e, filename, lbr.Start, rbr.End);
                }
                return e;
            case TokenType.LBRACE:
                t = lexer.Get();
                e = dictorsetmaker(t.Start);
                Expect(TokenType.RBRACE);
                return e;
            case TokenType.ID:
                t = lexer.Get();
                return new Identifier((string)t.Value!, filename, t.Start, t.End);
            case TokenType.STRING:
                t = lexer.Get();
                var start = t.Start;
                if (t.Value is Str str)
                {
                    for (; ; )
                    {
                        if (PeekAndDiscard(TokenType.COMMENT))
                            continue;
                        if (!Peek(TokenType.STRING))
                            break;
                        t = lexer.Get();
                        str = new Str(str.Value + ((Str)t.Value!).Value, filename, start, t.End);
                    }
                    return str;
                }
                var byteStr = (Bytes)t.Value!;
                while (Peek(TokenType.STRING))
                {
                    t = lexer.Get();
                    byteStr = new Bytes(byteStr.s + ((Bytes)t.Value!).s, filename, start, t.End);
                }
                return byteStr;
            case TokenType.INTEGER:
            case TokenType.LONGINTEGER:
                t = lexer.Get();
                return NumericLiteral(t);
            case TokenType.REAL:
                t = lexer.Get();
                return new RealLiteral((string)t.Value!, (double)t.NumericValue!, filename, t.Start, t.End);
            case TokenType.IMAG:
                t = lexer.Get();
                return new ImaginaryLiteral((string)t.Value!, (double)t.NumericValue!, filename, t.Start, t.End);
            case TokenType.ELLIPSIS:
                t = lexer.Get();
                return new Ellipsis(filename, t.Start, t.End);
            case TokenType.None:
                t = lexer.Get();
                return new NoneExp(filename, t.Start, t.End);
            case TokenType.False:
                t = lexer.Get();
                return new BooleanLiteral(false, filename, t.Start, t.End);
            case TokenType.True:
                t = lexer.Get();
                return new BooleanLiteral(true, filename, t.Start, t.End);
            default:
                return null;
            }
        }

        private Exp NumericLiteral(Token t)
        {
            if (t.NumericValue is BigInteger bignum)
            {
                return new BigLiteral((string)t.Value!, bignum, filename, t.Start, t.End);
            }
            else if (t.NumericValue is long l)
            {
                return new LongLiteral((string)t.Value!, l, filename, t.Start, t.End);
            }
            else if (t.NumericValue is int i)
            {
                return new IntLiteral((string)t.Value!, i, filename, t.Start, t.End);
            }
            else
                throw Error(Resources.ErrUnparseableIntegerToken, t);
        }

        //testlist_comp: (test|star_expr) ( comp_for | (',' (test|star_expr))* [','] )
        public Exp? testlist_comp(bool tuple)
        {
            Token startToken = lexer.Peek();
            if (startToken.Type == TokenType.RBRACKET || startToken.Type == TokenType.RPAREN)
            {
                var empty = new List<Exp>();
                if (tuple)
                    return new PyTuple(empty, filename, startToken.Start, startToken.End);
                else
                    return new PyList(empty, filename, startToken.Start, startToken.End);
            }

            Exp? e;
            if (Peek(TokenType.OP_STAR))
                e = star_expr();
            else
                e = test();
            if (Peek(TokenType.COMMENT))
            {
                var comment = (string) Expect(TokenType.COMMENT).Value!;
                if (e != null)
                    e.Comment = comment;
            }
            Exp e2;
            if (Peek(TokenType.For))
            {
                e2 = comp_for(e!);
                if (tuple)
                    return new GeneratorExp(e!, e2, filename, e!.Start, e2.End);
                else
                    return new ListComprehension(e2, filename, e!.Start, e2.End);
            }
            else
            {
                bool forceTuple = false;
                var exprs = new List<Exp>();
                if (e != null)
                    exprs.Add(e);
                while (PeekAndDiscard(TokenType.COMMA))
                {
                    while (PeekAndDiscard(TokenType.COMMENT))
                        ;
                    // Trailing comma forces a tuple.
                    if (Peek(TokenType.RBRACKET, TokenType.RPAREN))
                    {
                        forceTuple = true;
                        break;
                    }
                    if (Peek(TokenType.OP_STAR))
                        e = star_expr();
                    else
                        e = test();
                    if (e is null)
                        throw Unexpected();
                    exprs.Add(e);
                    if (Peek(TokenType.COMMENT))
                        e!.Comment = (string)Expect(TokenType.COMMENT).Value!;
                }
                var posStart = startToken.Start;
                var posEnd = exprs.Count > 0 ? exprs.Last().End : posStart;
                if (tuple)
                {
                    if (exprs.Count == 1 && !forceTuple)
                    {
                        return exprs[0];
                    }
                    else
                    {
                        return new PyTuple(exprs, filename, posStart, posEnd);
                    }
                }
                else
                {
                    return new PyList(exprs, filename, posStart, posEnd);
                }
            }
        }

        //trailer: '(' [arglist] ')' | '[' subscriptlist ']' | '.' NAME
        public Exp trailer(Exp core)
        {
            Token tok;
            switch (lexer.Peek().Type)
            {
            case TokenType.LPAREN:
                tok = lexer.Get();
                var args = arglist(core, tok.Start);
                Expect(TokenType.RPAREN);
                return args;
            case TokenType.LBRACKET:
                lexer.Get();
                var subs = subscriptlist();
                tok = Expect(TokenType.RBRACKET);
                return new ArrayRef(core, subs, filename, core.Start, tok.End);
            case TokenType.DOT:
                lexer.Get();
                tok = Expect(TokenType.ID);
                var id = new Identifier((string)tok.Value!, filename, core.Start, tok.End);
                return new AttributeAccess(core, id, filename, core.Start, tok.End);
            default:
                throw Unexpected();
            }
        }

        //subscriptlist: subscript (',' subscript)* [',']
        public List<Slice> subscriptlist()
        {
            var sub = subscript();
            var subs = new List<Slice> { sub };
            while (PeekAndDiscard(TokenType.COMMA))
            {
                if (Peek(TokenType.RBRACKET))
                    break;
                subs.Add(subscript());
            }
            return subs;
        }

        //subscript: test | [Fact] ':' [Fact] [sliceop]
        public Slice subscript()
        {
            Exp? start = null;
            Exp? end = null;
            Exp? stride = null;
            int lexPos = lexer.LineNumber;
            if (!Peek(TokenType.COLON))
            {
                start = test();
            }
            if (PeekAndDiscard(TokenType.COLON))
            {
                if (!Peek(TokenType.COLON, TokenType.RBRACKET))
                {
                    end = test();
                }
                if (Peek(TokenType.COLON))
                {
                    stride = sliceop();
                }
            }
            //$REVIEW: fix this [2:]
            return new Slice(start, end, stride,
                filename,
                lexPos,
                lexer.LineNumber);       //$BUG: should be position, not line number.
        }

        //sliceop: ':' [Fact]
        public Exp? sliceop()
        {
            Expect(TokenType.COLON);
            return test();
        }

        //exprlist: (expr|star_expr) (',' (expr|star_expr))* [',']
        public Exp exprlist()
        {
            var exprs = new List<Exp>();
            for (; ; )
            {
                Exp? e;
                if (Peek(TokenType.OP_STAR))
                    e = star_expr();
                else
                    e = expr();
                if (e is null)
                    throw Unexpected();
                exprs.Add(e);
                if (!PeekAndDiscard(TokenType.COMMA))
                    break;
                if (Peek(TokenType.In, TokenType.NEWLINE))
                    break;
            }
            if (exprs.Count == 1)
                return exprs[0];
            else
                return new ExpList(exprs, filename, exprs[0].Start, exprs.Last().End);
        }

        //testlist: test (',' test)* [',']
        public Exp testlist()
        {
            var exprs = new List<Exp>();
            for (; ; )
            {
                exprs.Add(ExpectTest());
                if (!PeekAndDiscard(TokenType.COMMA))
                    break;
                if (Peek(TokenType.COLON, TokenType.NEWLINE))
                    break;
            }
            return exprs.Count != 1 ? new ExpList(exprs, filename, exprs[0].Start, exprs.Last().End) : exprs[0];
        }

        //dictorsetmaker: ( (test ':' test (comp_for | (',' test ':' test)* [','])) |
        //                  (test (comp_for | (',' test)* [','])) )
        public Exp dictorsetmaker(int posStart)
        {
            int posEnd;
            var dictItems = new List<(Exp? Key, Exp Value)>();
            var setItems = new List<Exp>();
            while (PeekAndDiscard(TokenType.COMMENT))
                ;
            if (Peek(TokenType.RBRACE, out var token))
                return new DictInitializer(dictItems, filename, posStart, token.End);

            Exp? k = null;
            Exp? v;
            if (PeekAndDiscard(TokenType.OP_STARSTAR))
            {
                v = ExpectTest();
                dictItems.Add((null, v));
                posEnd = v.End;
            }
            else if (PeekAndDiscard(TokenType.OP_STAR))
            {
                v = ExpectTest();
                setItems.Add(new IterableUnpacker(v, filename, posStart, v.End));
                posEnd = v.End;
            }
            else
            {
                k = ExpectTest();
                posEnd = k.End;
            }
            if (dictItems.Count > 0 || PeekAndDiscard(TokenType.COLON))
            {
                if (k != null)
                {
                    v = ExpectTest();
                    if (Peek(TokenType.For))
                    {
                        var f = comp_for(null);
                        return new DictComprehension(k, v, f, filename, k.Start, f.End);
                    }
                    else
                    {
                        // dict initializer
                        dictItems.Add((k, v));
                        posEnd = v.End;
                    }
                }
                while (PeekAndDiscard(TokenType.COMMA))
                {
                    if (Peek(TokenType.RBRACE))
                        break;
                    if (PeekAndDiscard(TokenType.OP_STARSTAR))
                    {
                        v = ExpectTest();
                        dictItems.Add((null, v));
                        posEnd = v.End;
                    }
                    else
                    {
                        k = test();
                        if (k != null)
                        {
                            posEnd = k.End;
                            Expect(TokenType.COLON);
                            v = ExpectTest();
                            dictItems.Add((k, v));
                            posEnd = v.End;
                        }
                    }
                }
                return new DictInitializer(dictItems, filename, posStart, posEnd);
            }
            else
            {
                // set comprehension
                if (Peek(TokenType.For))
                {
                    var f = comp_for(k);
                    return new SetComprehension(f, filename, k!.Start, k.End);
                }
                else if (PeekAndDiscard(TokenType.OP_STARSTAR))
                {
                    v = ExpectTest();
                    dictItems.Add((null, v));
                    return new DictInitializer(dictItems, filename, posStart, v.End);
                }
                else
                {
                    if (k != null)
                        setItems.Add(k);
                    while (PeekAndDiscard(TokenType.COMMA))
                    {
                        while (PeekAndDiscard(TokenType.COMMENT))
                            ;
                        if (Peek(TokenType.RBRACE))
                            break;

                        if (PeekAndDiscard(TokenType.OP_STAR))
                        {
                            v = ExpectTest();
                            setItems.Add(new IterableUnpacker(v, filename, v.Start, v.End));
                        }
                        else
                        {
                            k = ExpectTest();
                            setItems.Add(k);
                        }
                    }
                    return new PySet(setItems, filename, setItems[0].Start, setItems[^1].End);
                }
            }
        }

        //classdef: 'class' NAME ['(' [arglist] ')'] ':' suite
        public List<Statement> classdef()
        {
            var posStart = Expect(TokenType.Class).Start;
            var name = id();
            Debug.Print("Parsing class {0}", name.Name);
            var args = new List<Argument>();
            if (!Peek(TokenType.COLON))
            {
                var token = Expect(TokenType.LPAREN);
                args = arglist(name, token.Start).Args;
                Expect(TokenType.RPAREN);
            }
            Expect(TokenType.COLON);
            var body = suite();
            return new List<Statement>
            {
                new ClassDef(name, args, body, filename, posStart, body.End)
            };
        }

        public Identifier id()
        {
            var token = Expect(TokenType.ID);
            return new Identifier((string)token.Value!, filename, token.Start, token.End);
        }

        //arglist: (argument ',')* (argument [',']
        //                         |'*' test (',' argument)* [',' '**' test] 
        //                         |'**' test)
        public Application arglist(Exp core, int posStart)
        {
            var args = new List<Argument>();
            var keywords = new List<Argument>();
            Exp? stargs = null;
            Exp? kwargs = null;
            if (Peek(TokenType.RPAREN, out var token))
                return new Application(core, args, keywords, stargs, kwargs, filename, core.Start, token.End);
            for (;;)
            {
                if (PeekAndDiscard(TokenType.OP_STAR))
                {
                    if (stargs != null)
                        throw Error(Resources.ErrMoreThanOneStargs);
                    stargs = test();
                }
                else if (PeekAndDiscard(TokenType.OP_STARSTAR))
                {
                    if (kwargs != null)
                        throw Error(Resources.ErrMoreThanOneKwargs);
                    kwargs = test();
                }
                else
                {
                    var arg = argument();
                    if (arg != null)
                        args.Add(arg);
                }

                if (!PeekAndDiscard(TokenType.COMMA, out token))
                    break;
                if (Peek(TokenType.RPAREN, out token))
                    break;
            }
            return new Application(core, args, keywords, stargs, kwargs, filename, posStart, token.End);
        }
        // The reason that keywords are test nodes instead of NAME is that using NAME
        // results in an ambiguity. ast.c makes sure it's a NAME.

        //argument: test [comp_for] | test '=' test  # Really [keyword '='] test
        public Argument? argument()
        {
            var argName = test();
            if (argName is null)
                return null;
            var posStart = argName.Start;
            var posEnd = argName.End;
            Exp? defval;
            if (Peek(TokenType.For))
            {
                CompFor f = comp_for(argName);
                return new Argument(null, f, filename, posStart, f.End);
            }
            else if (PeekAndDiscard(TokenType.EQ))
            {
                defval = ExpectTest();
                posEnd = defval.End;
            }
            else
            {
                defval = argName;
                argName = null;
            }
            return new Argument(argName, defval, filename, posStart, posEnd);
        }

        //comp_iter: comp_for | comp_if
        public CompIter comp_iter()
        {
            if (Peek(TokenType.For) || Peek(TokenType.Async))
                return comp_for(null);
            else
                return comp_if();
        }

        // comp_for: ['async'] sync_comp_for
        public CompFor comp_for(Exp? projection)
        {
            bool async = PeekAndDiscard(TokenType.Async);
            var compFor = sync_comp_for(projection);
            compFor.Async = async;
            return compFor;
        }

        //sync_comp_for: 'for' exprlist 'in' or_test [comp_iter]
        public CompFor sync_comp_for(Exp? projection)
        {
            var start = Expect(TokenType.For).Start;
            var exprs = exprlist();
            Expect(TokenType.In);
            var collection = or_test();
            if (collection is null)
                throw Unexpected();
            CompIter? next = null;
            if (Peek(TokenType.For, TokenType.If))
            {
                next = comp_iter();
            }
            return new CompFor(projection, exprs, collection, filename, (projection != null? projection.Start : start), (next ?? collection).End)
            {
                next = next,
            };
        }

        //comp_if: 'if' test_nocond [comp_iter]
        public CompIf comp_if()
        {
            var start = Expect(TokenType.If).Start;
            var test = test_nocond();
            if (test is null)
                throw Unexpected();
            CompIter? next = null;
            if (Peek(TokenType.For, TokenType.If))
                next = comp_iter();
            return new CompIf(test, filename, start, (next ?? test).End) 
            {
                next = next 
            };
        }

        // not used in grammar, but may appear in "node" passed from Parser to Compiler
        //encoding_decl: NAME

        //yield_expr: 'yield' [yield_arg]
        public Exp yield_expr()
        {
            var token = Expect(TokenType.Yield);
            if (!Peek(stmt_follow))
            {
                return yield_arg(token.Start);
            }
            else
            {
                return new YieldExp(null, filename, token.Start, token.End);
            }
        }

        //yield_arg: 'from' test | testlist_star_expr
        public Exp yield_arg(int posStart)
        {
            if (PeekAndDiscard(TokenType.From))
            {
                var t = test();
                if (t is null)
                    throw Unexpected();
                return new YieldFromExp(t, filename, posStart, t.End);
            }
            else
            {
                var tl = testlist_star_expr();
                return new YieldExp(tl, filename, posStart, tl.End);
            }
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}