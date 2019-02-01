using Pytocs.Core.TypeInference;
using Xunit;

#if DEBUG
namespace Pytocs.UnitTests.TypeInference
{
    public class FakeFileSystemTests
    {
        [Fact]
        public void Filesys_Add()
        {
            var fs = new FakeFileSystem()
                .Dir("foo")
                .File("foo.py", @"print('Howdy')" + "\r\n")
                .File("bar.py", @"def foo():\r\n    pass\r\n")
                .End()
                .Dir("end")
                .End();
            Assert.True(fs.FileExists("\\foo\\foo.py"));
            Assert.False(fs.FileExists("\\foo\\foo1.py"));
            Assert.False(fs.FileExists("\\bar\\foo1.py"));
        }
    }
}
#endif
