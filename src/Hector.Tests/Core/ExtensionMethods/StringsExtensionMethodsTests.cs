using FluentAssertions;
using Hector.Core;

namespace Hector.Tests.Core.ExtensionMethods
{
    public class StringsExtensionMethodsTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TestIsNullOrBlankStringTrue(string? s)
        {
            s.IsNullOrBlankString().Should().BeTrue();
        }

        [Theory]
        [InlineData("a")]
        public void TestIsNullOrBlankStringFalse(string s)
        {
            s.IsNullOrBlankString().Should().BeFalse();
        }

        [Theory]
        [InlineData("a")]
        public void TestIsNotNullAndNotBlankTrue(string s)
        {
            s.IsNotNullAndNotBlank().Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TestIsNotNullAndNotBlankFalse(string? s)
        {
            s.IsNotNullAndNotBlank().Should().BeFalse();
        }

        [Fact]
        public void TestStringJoin()
        {
            string[] values = ["hector", "is", "cool"];
            values.StringJoin(" ").Should().Be("hector is cool");
        }

        [Fact]
        public void TestShuffle()
        {
            string s = "hector is cool";
            s.Shuffle().Should().NotBe(s);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TestToNullIfBlank(string? s)
        {
            s.ToNullIfBlank().Should().Be(null);
        }

        [Theory]
        [InlineData("", 1, 2)]
        [InlineData(null, 1, 2)]
        [InlineData("test", 1, -1)]
        [InlineData("test", 8, 3)]
        [InlineData("test", 3, 2)]
        public void TestSafeSubstring(string? s, int startPos, int len)
        {
            s.SafeSubstring(startPos, len).Should().NotBeNull();
        }
    }
}
