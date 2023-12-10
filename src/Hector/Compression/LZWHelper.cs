using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Compression
{
    public class LZWHelper
    {
        public static byte[] Compress(string input)
        {
            LZWHelper lzw = new();
            int[] output = lzw.CompressString(input);
            return lzw.OutputToBytes(output);
        }

        public static string Decompress(byte[] input)
        {
            LZWHelper lzw = new();
            int[] output = lzw.BytesToOutput(input);
            string str = lzw.DecompressString(output);
            return str;
        }

        private int[] CompressString(string uncompressed)
        {
            Dictionary<string, int> dictionary = [];

            for (int i = 0; i < 256; ++i)
            {
                dictionary.Add(((char)i).ToString(), i);
            }

            string w = string.Empty;
            List<int> compressed = [];

            for (int i = 0; i < uncompressed.Length; ++i)
            {
                string wc = w + uncompressed[i];
                if (dictionary.ContainsKey(wc))
                {
                    w = wc;
                }
                else
                {
                    compressed.Add(dictionary[w]);
                    dictionary.Add(wc, dictionary.Count);
                    w = uncompressed[i].ToString();
                }
            }

            if (w.IsNotNullAndNotEmptyList())
            {
                compressed.Add(dictionary[w]);
            }

            return compressed.ToArray();
        }

        private string DecompressString(int[] compressed)
        {
            Dictionary<int, string> dictionary = [];

            for (int i = 0; i < 256; ++i)
            {
                dictionary.Add(i, ((char)i).ToString());
            }

            string w = dictionary[compressed[0]];
            StringBuilder decompressed = new(w);
            string? entry = null;

            for (int i = 1; i < compressed.Length; ++i)
            {
                entry = null;

                if (dictionary.TryGetValue(compressed[i], out string? value))
                {
                    entry = value;
                }
                else if (compressed[i] == dictionary.Count)
                {
                    entry = w + w[0];
                }

                decompressed.Append(entry);

                dictionary.Add(dictionary.Count, w + entry![0]);

                w = entry;
            }

            return decompressed.ToString();
        }

        private byte[] OutputToBytes(IList<int> output)
        {
            BitArray bits = new(output.ToArray());
            BitArray reducedBits = bits.ToMaxSignificantBits(out int maxSignBitIndex);
            byte[] outputBytes = reducedBits.ToByteArray();
            byte[] newBytes = new byte[outputBytes.Length + 1];
            newBytes[0] = ConvertIntToSingleByte(maxSignBitIndex);
            Array.Copy(outputBytes, 0, newBytes, 1, outputBytes.Length);
            return newBytes;
        }

        private int[] BytesToOutput(byte[] bytes)
        {
            int maxSignBitIndex = bytes[0];
            int maxBytesNumber = maxSignBitIndex + 1;

            BitArray bits = new(bytes.Skip(1).ToArray());

            var integerChunks =
                bits
                    .ToArray()
                    .Split(maxSignBitIndex + 1)
                    .Where(x => x.Length == maxBytesNumber)
                    .Select(x => new BitArray(x));

            int[] output =
                integerChunks
                    .Select(x => x.ToIntArray())
                    .SelectMany(x => x)
                    .ToArray();

            return output;
        }

        private static byte ConvertIntToSingleByte(int n)
        {
            if (n < 0 || n > 255)
            {
                throw new FormatException($"Unable to convert {n} to a single byte");
            }

            return (byte)n;
        }
    }
}
