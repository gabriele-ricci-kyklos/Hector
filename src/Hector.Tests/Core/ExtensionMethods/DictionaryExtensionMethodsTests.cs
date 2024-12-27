using FluentAssertions;

namespace Hector.Tests.Core.ExtensionMethods
{
    public class DictionaryExtensionMethodsTests
    {
        [Fact]
        public void TestGetValues()
        {
            Dictionary<int, int> dict = new()
            {
                {1, 1},
                {2, 2}
            };

            dict
                .GetValues([1, 2])
                .Should().Equal(1, 2);
        }
    }
}
