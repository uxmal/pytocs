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

using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pytocs.Core.TypeInference;

namespace Pytocs.UnitTests.TypeInference
{
    public class AnalyzerTests
    {
        private readonly Pytocs.Core.TypeInference.FakeFileSystem fs;
        private readonly ILogger logger;
        private readonly Dictionary<string, object> options;
        private readonly string nl;
        private readonly AnalyzerImpl an;

        public AnalyzerTests()
        {
            this.fs = new Pytocs.Core.TypeInference.FakeFileSystem();
            this.logger = new FakeLogger();
            this.options = new Dictionary<string, object>();
            this.nl = Environment.NewLine;
            this.an = new AnalyzerImpl(fs, logger, options, DateTime.Now);
        }

        private void ExpectBindings(string sExp)
        {
            var sActual = BindingsToString();
            if (sExp != sActual)
            {
                Console.WriteLine(sActual);
                var split = new string[] { nl };
                var opt = StringSplitOptions.None;
                var aExp = sExp.Split(split, opt);
                var aActual = sActual.Split(split, opt);
                int i;
                for (i = 0; i < Math.Min(aExp.Length, aActual.Length); ++i)
                {
                    Assert.Equal(aExp[i], aActual[i]);
                }
                Assert.False(i < aExp.Length, $"Fewer than the expected {aExp.Length} lines.");
                Assert.True(i > aExp.Length, $"More than the expected {aExp.Length} lines.");
                Assert.Equal(sExp, BindingsToString());
            }
        }

        [Fact]
        public void TypeAn_Empty()
        {
            an.Analyze("\\foo");
        }

        [Fact]
        public void TypeAn_StrDef()
        {
            fs.Dir("foo")
                .File("test.py", "x = 'hello world'\n");
            an.Analyze("\\foo");
            var sExp =
                @"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
                @"(binding:kind=SCOPE:node=x:type=str:qname=.foo.test.x:refs=[])" + nl;

            Assert.Equal(sExp, BindingsToString());
        }

        private string BindingsToString()
        {
            var sb = new StringBuilder();
            var e = an.GetAllBindings().Where(b => !b.IsBuiltin && !b.IsSynthetic).GetEnumerator();
            while (e.MoveNext())
            {
                sb.AppendLine(e.Current.ToString());
            }
            return sb.ToString();
        }

        [Fact]
        public void TypeAn_Copy()
        {
            fs.Dir("foo")
                .File("test.py", "x = 3\ny = x\n");
            an.Analyze(@"\foo");
            var sExp =
                @"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
                @"(binding:kind=SCOPE:node=x:type=int:qname=.foo.test.x:refs=[x])" + nl +
                @"(binding:kind=SCOPE:node=y:type=int:qname=.foo.test.y:refs=[])" + nl;
            Assert.Equal(sExp, BindingsToString());
        }

        [Fact]
        public void TypeAn_FuncDef()
        {
            fs.Dir("foo")
                .File("test.py",
@"
x = 'default'
def crunk(a):
    if x != 'default':
        print 'Yo'
    print a
    x = ''
    return 'fun'
");
            an.Analyze(@"\foo");
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=SCOPE:node=x:type=str:qname=.foo.test.x:refs=[x])" + nl +
@"(binding:kind=FUNCTION:node=crunk:type=? -> str:qname=.foo.test.crunk:refs=[])" + nl +
@"(binding:kind=SCOPE:node=x:type=str:qname=.foo.test.crunk.x:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=a:type=?:qname=.foo.test.crunk.a:refs=[a])" + nl +
@"(binding:kind=VARIABLE:node=x:type=str:qname=.foo.test.crunk.x:refs=[])" + nl;

            Console.WriteLine(BindingsToString());

            Assert.Equal(sExp, BindingsToString());
        }

        [Fact]
        public void TypeAn_FuncDef_Globals()
        {
            fs.Dir("foo")
                .File("test.py",
@"
x = 'default'
def crunk(a):
    global x
    if x != 'default':
        print 'Yo'
    print a
    x = ''
    return 'fun'
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=SCOPE:node=x:type=str:qname=.foo.test.x:refs=[x,x,x])" + nl +
@"(binding:kind=FUNCTION:node=crunk:type=? -> str:qname=.foo.test.crunk:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=a:type=?:qname=.foo.test.crunk.a:refs=[a])" + nl;

            Console.WriteLine(BindingsToString());

            Assert.Equal(sExp, BindingsToString());
        }

        [Fact]
        public void TypeAn_FwdReference()
        {
            fs.Dir("foo")
                .File("test.py",
@"
def foo(x):

    print 'foo ' + x,

    bar(x)


def bar(y):

    print 'bar ' + y


foo('Hello')
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=foo:type=str -> None:qname=.foo.test.foo:refs=[foo])" + nl +
@"(binding:kind=FUNCTION:node=bar:type=str -> None:qname=.foo.test.bar:refs=[bar])" + nl +
@"(binding:kind=PARAMETER:node=x:type=str:qname=.foo.test.foo.x:refs=[x,x])" + nl +
@"(binding:kind=PARAMETER:node=y:type=str:qname=.foo.test.bar.y:refs=[y])" + nl;

            Console.WriteLine(BindingsToString());

            Assert.Equal(sExp, BindingsToString());
        }

        [Fact]
        public void TypeAn_Attribute()
        {
            fs.Dir("foo")
                .File("test.py",
@"
class Foo(Object):
    def foo(self):
        print 'foo ' + self.x,

    self.bar(x)

    def bar(self, y):
        print 'bar ' + y

f = Foo('Hello')
f.foo()
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=CLASS:node=Foo:type=<Foo>:qname=.foo.test.Foo:refs=[Foo])" + nl +
@"(binding:kind=METHOD:node=foo:type=Foo -> None:qname=.foo.test.Foo.foo:refs=[f.foo])" + nl +
@"(binding:kind=PARAMETER:node=self:type=<Foo>:qname=.foo.test.Foo.foo.self:refs=[self])" + nl +
@"(binding:kind=METHOD:node=bar:type=(Foo, ?) -> None:qname=.foo.test.Foo.bar:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=self:type=<Foo>:qname=.foo.test.Foo.bar.self:refs=[])" + nl +
@"(binding:kind=SCOPE:node=f:type=Foo:qname=.foo.test.f:refs=[f])" + nl +
@"(binding:kind=PARAMETER:node=self:type=Foo:qname=.foo.test.Foo.foo.self:refs=[self])" + nl +
@"(binding:kind=PARAMETER:node=self:type=Foo:qname=.foo.test.Foo.bar.self:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=y:type=?:qname=.foo.test.Foo.bar.y:refs=[y])" + nl;

            Console.WriteLine(BindingsToString());

            Assert.Equal(sExp, BindingsToString());
        }

        [Fact]
        public void TypeAn_LocalVar()
        {
            fs.Dir("foo")
                .File("test.py",
@"
x = 'string'   # global var
def bar():
    x = 3      # local var
    print(x)
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=SCOPE:node=x:type=str:qname=.foo.test.x:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=bar:type=() -> None:qname=.foo.test.bar:refs=[])" + nl +
@"(binding:kind=SCOPE:node=x:type=int:qname=.foo.test.bar.x:refs=[x])" + nl +
@"(binding:kind=VARIABLE:node=x:type=int:qname=.foo.test.bar.x:refs=[x])" + nl;
            Console.Write(BindingsToString());
            Assert.Equal(sExp, BindingsToString());
        }

        [Fact]
        public void TypeAn_Point()
        {
            fs.Dir("foo")
                .File("test.py",
@"
def bar(point):
    return sqrt(point.x * point.x + point.y * point.y)
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=bar:type=? -> ?:qname=.foo.test.bar:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=point:type=?:qname=.foo.test.bar.point:refs=[point,point,point,point])" + nl;

            Console.Write(BindingsToString());
            Assert.Equal(sExp, BindingsToString());
        }

        [Fact]
        public void TypeAn_Dirs()
        {
            fs.Dir("sys_q")
                .Dir("parsing")
                    .File("__init__.py", "")
                    .File("parser.py",
@"
class Parser(object):
    def parse(self, phile):
        pass
")
                .End()
                .File("main.py",
@"
from parsing.parser import Parser

def mane_lupe(phile):
    p = Parser()
    p.parse(phile)
");
            an.Analyze(@"\sys_q");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\sys_q\parsing\__init__.py):type=:qname=:refs=[])" + nl +
@"(binding:kind=MODULE:node=(module:\sys_q\parsing\parser.py):type=parser:qname=.sys_q.parsing.parser:refs=[])" + nl +
@"(binding:kind=CLASS:node=Parser:type=<Parser>:qname=.sys_q.parsing.parser.Parser:refs=[Parser,Parser])" + nl +
@"(binding:kind=METHOD:node=parse:type=(Parser, ?) -> None:qname=.sys_q.parsing.parser.Parser.parse:refs=[p.parse])" + nl +
@"(binding:kind=PARAMETER:node=self:type=<Parser>:qname=.sys_q.parsing.parser.Parser.parse.self:refs=[])" + nl +

@"(binding:kind=MODULE:node=(module:\sys_q\main.py):type=main:qname=.sys_q.main:refs=[])" + nl +
@"(binding:kind=VARIABLE:node=parsing:type=:qname=:refs=[])" + nl +
@"(binding:kind=VARIABLE:node=parser:type=parser:qname=.sys_q.parsing.parser:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=mane_lupe:type=? -> None:qname=.sys_q.main.mane_lupe:refs=[])" + nl +
@"(binding:kind=SCOPE:node=p:type=Parser:qname=.sys_q.main.mane_lupe.p:refs=[p])" + nl +
@"(binding:kind=PARAMETER:node=self:type=Parser:qname=.sys_q.parsing.parser.Parser.parse.self:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=phile:type=?:qname=.sys_q.parsing.parser.Parser.parse.phile:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=phile:type=?:qname=.sys_q.main.mane_lupe.phile:refs=[phile])" + nl +
@"(binding:kind=VARIABLE:node=p:type=Parser:qname=.sys_q.main.mane_lupe.p:refs=[p])" + nl;
            ExpectBindings(sExp);
        }

        [Fact]
        public void TypeAn_class_instance_creation()
        {
            fs.Dir("foo")
                .File("cls.py",
@"
class Cls:
    def echo(self, s):
        print(s)
")
                .File("test.py",
@"
def bar():
    c = cls.Cls()
    c.echo(""Hello"")
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\cls.py):type=cls:qname=.foo.cls:refs=[])" + nl +
@"(binding:kind=CLASS:node=Cls:type=<Cls>:qname=.foo.cls.Cls:refs=[])" + nl +
@"(binding:kind=METHOD:node=echo:type=(Cls, ?) -> None:qname=.foo.cls.Cls.echo:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=self:type=<Cls>:qname=.foo.cls.Cls.echo.self:refs=[])" + nl +
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=bar:type=() -> None:qname=.foo.test.bar:refs=[])" + nl +
@"(binding:kind=SCOPE:node=c:type=?:qname=.foo.test.bar.c:refs=[c])" + nl +
@"(binding:kind=PARAMETER:node=self:type=Cls:qname=.foo.cls.Cls.echo.self:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=s:type=?:qname=.foo.cls.Cls.echo.s:refs=[s])" + nl +
@"(binding:kind=VARIABLE:node=c:type=?:qname=.foo.test.bar.c:refs=[c])" + nl;

            ExpectBindings(sExp);
        }

        [Fact]
        public void TypeAn_Array_Ref()
        {
            fs.Dir("foo")

                .File("test.py",
@"
def bar():
    s = ['bar']
    s[0] = 'foo'
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=bar:type=() -> None:qname=.foo.test.bar:refs=[])" + nl +
@"(binding:kind=SCOPE:node=s:type=[str]:qname=.foo.test.bar.s:refs=[s])" + nl +
@"(binding:kind=VARIABLE:node=s:type=[str]:qname=.foo.test.bar.s:refs=[s])" + nl;

            ExpectBindings(sExp);
        }

        [Fact]
        public void TypeAn_Bool_Local()
        {
            fs.Dir("foo")
                .File("test.py",
@"def fn():
    ret = True
    return ret
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=fn:type=() -> bool:qname=.foo.test.fn:refs=[])" + nl +
@"(binding:kind=SCOPE:node=ret:type=bool:qname=.foo.test.fn.ret:refs=[ret])" + nl +
@"(binding:kind=VARIABLE:node=ret:type=bool:qname=.foo.test.fn.ret:refs=[ret])" + nl;

            ExpectBindings(sExp);
        }


        [Fact]
        public void TypeAn_Inherit_field()
        {
            fs.Dir("foo")
                .File("test.py",
@"class Base():
    def __init__():
        this.field = ""hello""

class Derived(Base):
    def foo():
        print(this.field)
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=CLASS:node=Base:type=<Base>:qname=.foo.test.Base:refs=[Base])" + nl +
@"(binding:kind=CONSTRUCTOR:node=__init__:type=() -> None:qname=.foo.test.Base.__init__:refs=[])" + nl +
@"(binding:kind=CLASS:node=Derived:type=<Derived>:qname=.foo.test.Derived:refs=[])" + nl +
@"(binding:kind=METHOD:node=foo:type=() -> None:qname=.foo.test.Derived.foo:refs=[])" + nl;

            ExpectBindings(sExp);
        }

        // Reported in https://github.com/uxmal/pytocs/issues/51
        [Fact(DisplayName = nameof(TypeAn_async_function))]
        public void TypeAn_async_function()
        {
            fs.Dir("foo")
                .File("test.py",
@"async def foo(field) -> bool:
    return field == ""hello""
");
            an.Analyze(@"\foo");
            an.Finish();
            var sExp =
@"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
@"(binding:kind=FUNCTION:node=foo:type=? -> bool:qname=.foo.test.foo:refs=[])" + nl +
@"(binding:kind=PARAMETER:node=field:type=?:qname=foo.field:refs=[field])" + nl;
            ExpectBindings(sExp);
        }

    }
}
