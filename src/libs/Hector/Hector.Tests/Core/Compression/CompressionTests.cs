using FluentAssertions;
using Hector.Core.Compression;

namespace Hector.Tests.Core.Compression
{
    public class CompressionTests
    {
        [Theory]
        [InlineData("hector")]
        [InlineData("c# programming language")]
        public async Task TestGZipHelper(string input)
        {
            byte[] compressed = await GZipHelper.CompressStringAsync(input);
            string decompressed = await GZipHelper.DecompressStringAsync(compressed);

            decompressed.Should().Be(input);
        }

        [Theory]
        [InlineData("hector")]
        [InlineData("c# programming language")]
        public void TestLZ77Helper(string input)
        {
            string compressed = LZ77Helper.CompressString(input);
            string decompressed = LZ77Helper.DecompressStrings(compressed);

            decompressed.Should().Be(input);
        }

        [Theory]
        [InlineData("hector")]
        [InlineData("c# programming language")]
        public void TestLZWHelper(string input)
        {
            byte[] compressed = LZWHelper.Compress(input);
            string decompressed = LZWHelper.Decompress(compressed);

            decompressed.Should().Be(input);
        }
    }
}
