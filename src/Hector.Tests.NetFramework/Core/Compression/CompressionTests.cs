using FluentAssertions;
using Hector.Compression;
using Xunit;

namespace Hector.Tests.NetFramework.Core.Compression
{
    public class CompressionTests
    {
        [Theory]
        [InlineData("hector")]
        [InlineData("c# programming language")]
        public void TestLZ77Helper(string input)
        {
            string compressed = LZ77Helper.CompressString(input);
            string decompressed = LZ77Helper.DecompressStrings(compressed);

            decompressed.Should().Be(input);
        }
    }
}
