using FluentAssertions;

namespace Hector.Tests.Core.ExtensionMethods
{
    public class MiscExtensionMethodsTests
    {
        [Theory]
        [InlineData(1, typeof(int))]
        [InlineData(1, typeof(int?))]
        [InlineData("a", typeof(string))]
        public void TestConvertTo(object value, Type typeTo)
        {
            object obj = value.ConvertTo(typeTo);
            obj.GetType().Should().Match(x => x == typeTo || x == Nullable.GetUnderlyingType(typeTo));
        }

        [Fact]
        public void TestToEnum()
        {
            const string right = "Network";
            const string wrong = "Networ";

            right.ToEnum<DriveType>().Should().NotBeNull();
            wrong.ToEnum<DriveType>().Should().BeNull();
        }
    }
}
