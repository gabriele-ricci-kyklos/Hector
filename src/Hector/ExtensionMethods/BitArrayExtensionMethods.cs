using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Hector.Core
{
    public static class BitArrayExtensionMethods
    {
        //credits: https://stackoverflow.com/a/14354311/4499267
        public static int GetCardinality(this BitArray bitArray)
        {
            int[] ints = new int[(bitArray.Count >> 5) + 1];

            bitArray.CopyTo(ints, 0);

            int count = 0;

            // fix for not truncated bits in last integer that may have been set to true with SetAll()
            ints[^1] &= ~(-1 << (bitArray.Count % 32));

            for (int i = 0; i < ints.Length; i++)
            {

                int c = ints[i];

                // magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
                unchecked
                {
                    c = c - ((c >> 1) & 0x55555555);
                    c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
                    c = ((c + (c >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
                }

                count += c;

            }

            return count;
        }

        //credits: https://stackoverflow.com/a/37072636/4499267
        public static int IndexOfMaxSignificantBit(this BitArray array)
        {
            int[] intArray = (int[])array.GetType().GetField("m_array", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(array);
            int pos = -1;
            int maxPos = -1;

            for (int j = 0; j < intArray.Length; ++j)
            {
                var b = intArray[j];
                if (b != 0)
                {
                    pos = 31;
                    for (int bit = 31; bit >= 0; --bit)
                    {
                        if ((b & (1 << bit)) != 0)
                        {
                            break;
                        }

                        pos--;
                    }
                }

                maxPos = Math.Max(maxPos, pos);
            }

            return maxPos;
        }

        public static BitArray ToMaxSignificantBits(this BitArray bitArray) =>
            bitArray.ToMaxSignificantBits(out _);

        public static BitArray ToMaxSignificantBits(this BitArray bitArray, out int maxSignificantBit)
        {
            maxSignificantBit = bitArray.IndexOfMaxSignificantBit();
            List<bool> list = new();

            for (int j = 0; j < bitArray.Length; j += 32)
            {
                for (int i = j; i < (j + 32); ++i)
                {
                    if (i <= (maxSignificantBit + j))
                    {
                        list.Add(bitArray[i]);
                    }
                }
            }

            return new BitArray(list.ToArray());
        }

        public static bool[] ToArray(this BitArray bitArray)
        {
            bool[] bits = new bool[bitArray.Count];
            bitArray.CopyTo(bits, 0);

            return bits;
        }

        public static byte[] ToByteArray(this BitArray bits)
        {
            int length = Math.Max(1, Math.Ceiling((double)bits.Length / 8).ConvertTo<int>());
            byte[] bytes = new byte[length];
            bits.CopyTo(bytes, 0);
            return bytes;
        }

        public static int[] ToIntArray(this BitArray bits)
        {
            int length = Math.Max(1, Math.Ceiling((double)bits.Length / 32).ConvertTo<int>());
            int[] ints = new int[length];
            bits.CopyTo(ints, 0);
            return ints;
        }
    }
}
