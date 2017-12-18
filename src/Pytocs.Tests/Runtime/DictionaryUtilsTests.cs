using pytocs.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Pytocs.UnitTests.Runtime
{
    public class DictionaryUtilsTests
    {
        [Fact(DisplayName = nameof(Du_AddTuple))]
        public void Du_AddTuple()
        {
            var dict = DictionaryUtils.Unpack<string, object>(("a", "b"));
            Assert.Single(dict);
            Assert.Equal("b", dict["a"]);
        }

        [Fact(DisplayName = nameof(Du_AddDict))]
        public void Du_AddDict()
        {
            var dictSrc = new Dictionary<string, object>
            {
                { "b", "c" }
            };
            var dict = DictionaryUtils.Unpack<string, object>(("a", "b"), dictSrc);
            Assert.Equal(2, dict.Count());
            Assert.Equal("b", dict["a"]);
        }
    }
}
