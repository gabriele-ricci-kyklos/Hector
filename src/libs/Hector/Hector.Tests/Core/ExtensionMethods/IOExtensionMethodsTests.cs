using FluentAssertions;
using Hector.Core;

namespace Hector.Tests.Core.ExtensionMethods
{
    public class IOExtensionMethodsTests
    {
        [Theory]
        [InlineData("my", "folder")]

        public void TestCombinePaths(string path1, string path2)
        {
            "C:".CombinePaths(path1, path2).Should().Be(@"C:\my\folder");
        }

        [Fact]
        public void TestIsUncPath()
        {
            @"\\MYSERVER\my\folder".IsUncPath().Should().BeTrue();
            @"C:\my\folder".IsUncPath().Should().BeFalse();
        }

        [Fact]
        public void TestIsAbsolutePath()
        {
            @"C:\my\folder".IsAbsolutePath().Should().BeTrue();
            @"..\my\folder".IsAbsolutePath().Should().BeFalse();
        }
    }
}
