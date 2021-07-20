using Hector.Core.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Compression
{
    /*
     * Started using the code from https://rosettacode.org/wiki/LZW_compression#C.23
    */

    public class LZW
    {
        public static byte[] Compress(string input)
        {
            LZW lzw = new LZW();
            IList<int> output = lzw.CompressString(input);
            return lzw.OutputToBytes(output);
        }

        public static string Decompress(byte[] input)
        {
            LZW lzw = new LZW();
            IList<int> output = lzw.BytesToOutput(input);
            string str = lzw.DecompressString(output);
            return str;
        }

        private IList<int> CompressString(string uncompressed)
        {
            IDictionary<string, int> dictionary = new Dictionary<string, int>();

            for (int i = 0; i < 256; ++i)
            {
                dictionary.Add(((char)i).ToString(), i);
            }

            string w = string.Empty;
            IList<int> compressed = new List<int>();

            for(int i = 0; i < uncompressed.Length; ++i)
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
            
            if (!w.IsNullOrEmpty())
            {
                compressed.Add(dictionary[w]);
            }

            return compressed;
        }

        private string DecompressString(IList<int> compressed)
        {
            IDictionary<int, string> dictionary = new Dictionary<int, string>();

            for (int i = 0; i < 256; ++i)
            {
                dictionary.Add(i, ((char)i).ToString());
            }

            string w = dictionary[compressed[0]];
            compressed.RemoveAt(0);
            StringBuilder decompressed = new StringBuilder(w);
            string entry = null;

            for (int i = 0; i < compressed.Count; ++i)
            {
                entry = null;

                if (dictionary.ContainsKey(compressed[i]))
                {
                    entry = dictionary[compressed[i]];
                }
                else if (compressed[i] == dictionary.Count)
                {
                    entry = w + w[0];
                }

                decompressed.Append(entry);

                dictionary.Add(dictionary.Count, w + entry[0]);

                w = entry;
            }

            return decompressed.ToString();
        }

        private byte[] OutputToBytes(IList<int> output)
        {
            output.AssertNotNull("output");

            BitArray bits = new BitArray(output.ToArray());
            int maxSignBitIndex;
            BitArray reducedBits = bits.ToMaxSignificantBits(out maxSignBitIndex);
            byte[] outputBytes = reducedBits.ToByteArray();
            byte[] newBytes = new byte[outputBytes.Length + 1];
            newBytes[0] = ConvertIntToSingleByte(maxSignBitIndex);
            Array.Copy(outputBytes, 0, newBytes, 1, outputBytes.Length);
            return newBytes;
        }

        private IList<int> BytesToOutput(byte[] bytes)
        {
            bytes.AssertNotNull("bytes");

            int maxSignBitIndex = (int)bytes[0];
            int maxBytesNumber = maxSignBitIndex + 1;

            BitArray bits = new BitArray(bytes.Skip(1).ToArray());

            var integerChunks = 
                bits
                    .ToArray()
                    .Split(maxSignBitIndex + 1)
                    .Where(x => x.Count() == maxBytesNumber)
                    .Select(x => new BitArray(x));

            IList<int> output = 
                integerChunks
                    .Select(x => x.ToIntArray())
                    .SelectMany(x => x)
                    .ToList();

            return output;
        }

        private byte ConvertIntToSingleByte(int n)
        {
            if(n < 0 || n > 255)
            {
                throw new FormatException($"Unable to convert {n} to a single byte");
            }

            return (byte)n;
        }
    }
}
