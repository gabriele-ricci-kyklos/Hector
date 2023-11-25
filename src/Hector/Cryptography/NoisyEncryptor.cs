using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Cryptography
{
    public class NoisyEncryptor
    {
        private readonly Random _random;
        private readonly char[] _charsForZero;
        private readonly char[] _charsForOne;
        private readonly char[] _charsInTheMiddle;

        public string String { get; set; }
        public int Noise { get; set; }

        private NoisyEncryptor(string str, int noise)
        {
            _random = new Random();
            _charsForZero = "/[]{~)@#_,;:".ToCharArray();
            _charsForOne = "+-%?}<>(!".ToCharArray();
            _charsInTheMiddle = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&|*^=".ToCharArray();

            String = str;
            Noise = noise;
        }

        public static string Encode(string str, int inMiddleCharsStrength = 0)
        {
            NoisyEncryptor obj = new(str, inMiddleCharsStrength);
            string encodedStr = obj.Encode();
            return encodedStr;
        }

        public static string Decode(string str)
        {
            NoisyEncryptor obj = new(str, 0);
            string decodedStr = obj.Decode();
            return decodedStr;
        }

        private string Encode()
        {
            string workedString =
                BinaryString
                    .FromString(String, Encoding.UTF8)
                    .InvertBinaries()
                    .ReverseBinaries()
                    .ToString();

            StringBuilder buffer = new(workedString.Length * 2);
            int index;

            foreach (char c in workedString)
            {
                if (c == '0')
                {
                    index = _random.Next(_charsForZero.Length);
                    buffer.Append(_charsForZero[index]);
                }
                else
                {
                    index = _random.Next(_charsForOne.Length);
                    buffer.Append(_charsForOne[index]);
                }

                if (_random.Next(100) > 100 - Noise)
                {
                    index = _random.Next(_charsInTheMiddle.Length);
                    buffer.Append(_charsInTheMiddle[index]);
                }
            }

            return buffer.ToString();
        }

        private string Decode()
        {
            HashSet<char> charsInTheMiddle = _charsInTheMiddle.ToHashSet();
            HashSet<char> charsForZero = _charsForZero.ToHashSet();
            HashSet<char> charsForOne = _charsForOne.ToHashSet();

            StringBuilder buffer = new(String.Length);

            foreach (char c in String.Where(x => !charsInTheMiddle.Contains(x)))
            {
                if (charsForZero.Contains(c))
                {
                    buffer.Append('0');
                }
                else if (charsForOne.Contains(c))
                {
                    buffer.Append('1');
                }
                else
                {
                    throw new ArgumentException($"Invalid character '{c}'");
                }
            }

            return
                BinaryString
                    .FromBinaryString(buffer.ToString())
                    .ReverseBinaries()
                    .InvertBinaries()
                    .OriginalString;
        }
    }
    class BinaryString
    {
        private readonly Encoding _encoding;
        private readonly string _binaryString;

        public byte[] Bytes { get; set; }
        public string OriginalString => _encoding.GetString(Bytes);

        public static BinaryString FromString(string str, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] bytes = encoding.GetBytes(str);
            return new(bytes, encoding);
        }

        public static BinaryString FromByteArray(byte[] byteArray, Encoding? encoding = null) =>
            new(byteArray, encoding);

        public static BinaryString FromBinaryString(string binaryString, Encoding? encoding = null)
        {
            if (binaryString.Any(x => x != '0' && x != '1'))
            {
                throw new ArgumentException($"The provided string '{binaryString}' is not a binary string");
            }

            byte[] byteArray = GetByteArrayFromBinaryString(binaryString);

            return new BinaryString(byteArray, encoding);
        }

        private BinaryString(byte[] byteArray, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            Bytes = byteArray;

            _encoding = encoding;
            _binaryString =
                string
                .Join
                (
                    string.Empty,
                    byteArray
                        .Select(x => Convert.ToString(x, 2).PadLeft(8, '0'))
                );
        }

        public BinaryString InvertBinaries()
        {
            StringBuilder buffer = new();

            foreach (char c in _binaryString)
            {
                buffer.Append((c == '1') ? '0' : '1');
            }

            string invertedBinaryStr = buffer.ToString();
            byte[] bytes = GetByteArrayFromBinaryString(invertedBinaryStr);

            return new BinaryString(bytes);
        }

        public BinaryString ReverseBinaries()
        {
            string[] strArray =
                _binaryString
                    .ToCharArray()
                    .Select(x => x.ToString())
                    .ToArray();

            Array.Reverse(strArray);
            byte[] bytes = GetByteArrayFromBinaryString(string.Join(string.Empty, strArray));

            return new BinaryString(bytes);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not BinaryString binStr)
            {
                return false;
            }

            return OriginalString.Equals(binStr.OriginalString);
        }

        public override int GetHashCode() => OriginalString.GetHashCode();

        public override string ToString() => _binaryString;

        private static byte[] GetByteArrayFromBinaryString(string binaryStr)
        {
            int numOfBytes = binaryStr.Length / 8;
            byte[] bytes = new byte[numOfBytes];

            for (int i = 0; i < numOfBytes; ++i)
            {
                bytes[i] = Convert.ToByte(binaryStr.Substring(8 * i, 8), 2);
            }

            return bytes;
        }
    }
}