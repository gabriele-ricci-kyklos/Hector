using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Compression
{
    public static class GZipHelper
    {
        public static async Task CompressAsync(Stream inputStream, Stream outputStream)
        {
            using GZipStream zipStream = new(outputStream, CompressionMode.Compress, false);
            await inputStream.CopyToAsync(zipStream).ConfigureAwait(false);
        }

        public static async Task DecompressAsync(Stream inputStream, Stream outputStream)
        {
            using GZipStream gzipStream = new(inputStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(outputStream).ConfigureAwait(false);
        }

        public static async Task<byte[]> CompressBytesAsync(byte[] bytes)
        {
            using MemoryStream outputStream = new();
            using MemoryStream inputStream = new(bytes);
            await CompressAsync(inputStream, outputStream).ConfigureAwait(false);
            return outputStream.ToArray();
        }

        public static async Task<byte[]> DecompressBytesAsync(byte[] bytes)
        {
            using MemoryStream inputStream = new(bytes);
            using MemoryStream outputStream = new();
            await DecompressAsync(inputStream, outputStream).ConfigureAwait(false);
            return outputStream.ToArray();
        }

        public static Task<byte[]> CompressStringAsync(string str, Encoding? encoding = null)
        {
            byte[] strBytes = (encoding ??= Encoding.UTF8).GetBytes(str);
            return CompressBytesAsync(strBytes);
        }

        public static async Task<string> DecompressStringAsync(byte[] bytes, Encoding? encoding = null)
        {
            byte[] decompressedBytes = await DecompressBytesAsync(bytes).ConfigureAwait(false);
            return (encoding ??= Encoding.UTF8).GetString(decompressedBytes);
        }
    }
}
