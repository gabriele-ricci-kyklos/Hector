using FluentAssertions;
using Hector.Core.Compression;
using Hector.Core.Cryptography;
using System.Text;

namespace Hector.Tests.Core.Cryptography
{
    public class CryptographyTests
    {
        private const string _input = "hector";
        private byte[] _inputBytes = Encoding.UTF8.GetBytes(_input);

        [Fact]
        public void TestHashHelperMD5()
        {
            const string expectedOutput = "3ab9071536d62f29aa8b3fd39141f6ad";

            string hash = HashHelper.ComputeMD5(_input);
            hash.Should().Be(expectedOutput);

            string bHash = HashHelper.ComputeMD5(_inputBytes);
            bHash.Should().Be(expectedOutput);
        }

        [Fact]
        public void TestHashHelperSHA1()
        {
            const string expectedOutput = "9c6eb1131b4202ad199765465391a99f6498009c";

            string hash = HashHelper.ComputeSHA1(_input);
            hash.Should().Be(expectedOutput);

            string bHash = HashHelper.ComputeSHA1(_inputBytes);
            bHash.Should().Be(expectedOutput);
        }

        [Fact]
        public void TestHashHelperSHA256()
        {
            const string expectedOutput = "51d3ba50d3e136bc03ca019303427831f4f49d88b775b4a529685533c8ce0e65";

            string hash = HashHelper.ComputeSHA256(_input);
            hash.Should().Be(expectedOutput);

            string bHash = HashHelper.ComputeSHA256(_inputBytes);
            bHash.Should().Be(expectedOutput);
        }

        [Fact]
        public void TestHashHelperSHA5121()
        {
            const string expectedOutput = "150fabdef322b295706ecfa27acbfd36496ed754bd27342323d42711da174075575d2a53ef538e37945c19edb3968bb399ce4a6d9cdd80e841d650ed81551c08";

            string hash = HashHelper.ComputeSHA512(_input);
            hash.Should().Be(expectedOutput);

            string bHash = HashHelper.ComputeSHA512(_inputBytes);
            bHash.Should().Be(expectedOutput);
        }

        [Theory]
        [InlineData("hector")]
        [InlineData("c# programming language")]
        public void TestNoisyEncryptor(string input)
        {
            string compressed = NoisyEncryptor.Encode(input, 10);
            string decompressed = NoisyEncryptor.Decode(compressed);

            decompressed.Should().Be(input);
        }
    }
}
