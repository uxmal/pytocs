using System.Collections.Generic;
using System.Linq;
using Runtime;
using Xunit;

namespace Pytocs.UnitTests.Runtime
{
    public class DictionaryUtilsTests
    {
        [Fact(DisplayName = nameof(Du_AddDict))]
        public void Du_AddDict()
        {
            Dictionary<string, object> dictSrc = new Dictionary<string, object>
            {
                {"b", "c"}
            };
            Dictionary<string, object> dict = DictionaryUtils.Unpack<string, object>(("a", "b"), dictSrc);
            Assert.Equal(2, dict.Count());
            Assert.Equal("b", dict["a"]);
        }

        [Fact(DisplayName = nameof(Du_AddTuple))]
        public void Du_AddTuple()
        {
            Dictionary<string, object> dict = DictionaryUtils.Unpack<string, object>(("a", "b"));
            Assert.Single(dict);
            Assert.Equal("b", dict["a"]);
        }
    }
}