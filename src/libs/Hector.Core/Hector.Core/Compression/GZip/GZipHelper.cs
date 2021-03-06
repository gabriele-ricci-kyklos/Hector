using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Core.Compression.GZip
{
    public static class GZipHelper
    {
        public static async Task<MemoryStream> CompressAsync(Stream inputStream)
        {
            MemoryStream resultStream = new();
            using (GZipStream zipStream = new(resultStream, CompressionMode.Compress, false))
            {
                await inputStream.CopyToAsync(zipStream).ConfigureAwait(false);
            }

            return resultStream;
        }

        public static async Task<MemoryStream> DecompressAsync(Stream inputStream)
        {
            MemoryStream resultStream = new();

            using (GZipStream gzipStream = new(inputStream, CompressionMode.Decompress))
            {
                await gzipStream.CopyToAsync(resultStream).ConfigureAwait(false);
            }

            return resultStream;
        }

        public static async Task<byte[]> CompressStringAsync(string s, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] stringBytes = encoding.GetBytes(s);
            using MemoryStream stringStream = new(stringBytes);
            using MemoryStream resultStream = await CompressAsync(stringStream).ConfigureAwait(false);
            byte[] gzipBytes = resultStream.ToArray();
            return gzipBytes;
        }

        public static async Task<string> DecompressStringAsync(byte[] bytes, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            using MemoryStream inputStream = new(bytes);
            using MemoryStream resultStream = await DecompressAsync(inputStream).ConfigureAwait(false);
            byte[] unzippedBytes = resultStream.ToArray();
            return encoding.GetString(unzippedBytes);
        }
    }
}
