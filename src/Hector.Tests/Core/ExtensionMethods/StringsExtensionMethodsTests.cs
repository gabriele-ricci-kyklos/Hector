using FluentAssertions;
using System.Globalization;

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

        [Theory]
        [InlineData("123", typeof(byte), (byte)123)]
        [InlineData("abc", typeof(byte), null)] // Invalid format
        [InlineData("  -123.45  ", typeof(decimal), -123.45)] // Whitespace
        [InlineData("abc", typeof(decimal), null)] // Invalid format
        [InlineData("-9999.99", typeof(double), -9999.99)]
        [InlineData("abc", typeof(double), null)] // Invalid format
        [InlineData("123", typeof(short), (short)123)]
        [InlineData("abc", typeof(short), null)] // Invalid format
        [InlineData("123", typeof(int), 123)]
        [InlineData("abc", typeof(int), null)] // Invalid format
        [InlineData("123", typeof(long), 123L)]
        [InlineData("abc", typeof(long), null)] // Invalid format
        [InlineData("123", typeof(sbyte), (byte)123)]
        [InlineData("abc", typeof(sbyte), null)] // Invalid format
        [InlineData("-9999.99", typeof(float), -9999.99f)]
        [InlineData("abc", typeof(float), null)] // Invalid format
        [InlineData("123", typeof(ushort), (ushort)123)]
        [InlineData("abc", typeof(ushort), null)] // Invalid format
        [InlineData("123", typeof(uint), 123U)]
        [InlineData("abc", typeof(uint), null)] // Invalid format
        [InlineData("123", typeof(ulong), 123UL)]
        [InlineData("abc", typeof(ulong), null)] // Invalid format
        [InlineData("123", typeof(string), null)] // Non-numeric type
        [InlineData("1,234.56", typeof(double), 1234.56)] // Thousands separator in US culture
        [InlineData("(123.45)", typeof(decimal), -123.45)] // Parentheses for negative
        public void ToNumber_Tests(string input, Type type, object? expected, IFormatProvider? formatProvider = null)
        {
            input.ToNumber(type, formatProvider).Should().Be(expected);
        }
    }
}
