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
            const string ignoreCaseRight = "network";

            right.ToEnum<DriveType>().Should().NotBeNull();
            wrong.ToEnum<DriveType>().Should().BeNull();

            ignoreCaseRight.ToEnum<DriveType>(true).Should().NotBeNull();
            ignoreCaseRight.ToEnum<DriveType>().Should().BeNull();
        }

        [Fact]
        public void TestTryParseToEnum()
        {
            const string right = "Network";
            const string wrong = "Networ";
            const string ignoreCaseRight = "network";

            right.TryParseToEnum(out DriveType d).Should().BeTrue();
            d.Should().Be(DriveType.Network);

            wrong.TryParseToEnum<DriveType>(out _).Should().BeFalse();

            ignoreCaseRight.TryParseToEnum(out DriveType ddd, true).Should().BeTrue();
            ddd.Should().Be(DriveType.Network);

            ignoreCaseRight.TryParseToEnum<DriveType>(out _).Should().BeFalse();
        }
    }
}
