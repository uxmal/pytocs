#if DEBUG
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pytocs.TypeInference
{
    [TestFixture]
    public class AnalyzerTests
    {
        private Pytocs.TypeInference.FakeFileSystem fs;
        private Dictionary<string, object> options;
        private string nl;
        private AnalyzerImpl an;

        [SetUp]
        public void Setup()
        {
            this.fs = new Pytocs.TypeInference.FakeFileSystem();
            this.options = new Dictionary<string, object>();
            this.nl = Environment.NewLine;
            this.an = new AnalyzerImpl(fs, options, DateTime.Now);
        }

        [Test]
        public void Empty()
        {
            an.Analyze("\\foo");
        }

        [Test]
        public void StrDef()
        {
            fs.Dir("foo")
                .File("test.py", "x = 'hello world'\n");
            an.Analyze("\\foo");
            var sExp =
                @"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
                @"(binding:kind=SCOPE:node=x:type=str:qname=.foo.test.x:refs=[])" + nl;

            Assert.AreEqual(sExp, BindingsToString());
        }

        private string BindingsToString()
        {
            var sb = new StringBuilder();
            var e = an.getAllBindings().Where(b => !b.IsBuiltin).GetEnumerator();
            while (e.MoveNext())
            {
                sb.AppendLine(e.Current.ToString());
            }
            return sb.ToString();
        }

        [Test]
        public void TypeAn_Copy()
        {
            fs.Dir("foo")
                .File("test.py", "x = 3\ny = x\n");
            an.Analyze(@"\foo");
            var sExp =
                @"(binding:kind=MODULE:node=(module:\foo\test.py):type=test:qname=.foo.test:refs=[])" + nl +
                @"(binding:kind=SCOPE:node=x:type=int:qname=.foo.test.x:refs=[x])" + nl +
                @"(binding:kind=SCOPE:node=y:type=int:qname=.foo.test.y:refs=[])" + nl;
            Assert.AreEqual(sExp, BindingsToString());
        }

        [Test]
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
@"(binding:kind=FUNCTION:node=crunk:type=? -> ?:qname=.foo.test.crunk:refs=[])" + nl +
@"(binding:kind=SCOPE:node=x:type=str:qname=.foo.test.crunk.x:refs=[])" + nl;

            Console.WriteLine(BindingsToString());

            Assert.AreEqual(sExp, BindingsToString());
        }

        [Test]
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

            Assert.AreEqual(sExp, BindingsToString());
        }

        [Test]
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

            Assert.AreEqual(sExp, BindingsToString());
        }

        [Test]
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
@"(binding:kind=FUNCTION:node=foo:type=str -> None:qname=.foo.test.foo:refs=[foo])" + nl +
@"(binding:kind=FUNCTION:node=bar:type=str -> None:qname=.foo.test.bar:refs=[bar])" + nl +
@"(binding:kind=PARAMETER:node=x:type=str:qname=.foo.test.foo.x:refs=[x,x])" + nl +
@"(binding:kind=PARAMETER:node=y:type=str:qname=.foo.test.bar.y:refs=[y])" + nl;

            Console.WriteLine(BindingsToString());

            Assert.AreEqual(sExp, BindingsToString());
        }

    }
}
#endif