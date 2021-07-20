using Hector.Core.Support.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Cryptography
{
    public class GenericEncryption
    {
        private Random _random;
        private char[] _charsForZero;
        private char[] _charsForOne;
        private char[] _charsInTheMiddle;

        public string String { get; set; }
        public int InMiddleCharsStrength { get; set; }

        private GenericEncryption(string str, int inMiddleCharsStrength)
        {
            _random = new Random();
            _charsForZero = "/[]{~)@#_,;:".ToCharArray();
            _charsForOne = "+-%?}<>(!".ToCharArray();
            _charsInTheMiddle = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&|*^=".ToCharArray();

            String = str;
            InMiddleCharsStrength = inMiddleCharsStrength;
        }

        public static string FromString(string str, int inMiddleCharsStrength = 0)
        {
            GenericEncryption obj = new GenericEncryption(str, inMiddleCharsStrength);
            string encodedStr = obj.Encode();
            return encodedStr;
        }

        public static string FromEncryptedString(string str)
        {
            GenericEncryption obj = new GenericEncryption(str, 0);
            string decodedStr = obj.Decode();
            return decodedStr;
        }

        private string Encode()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(String);
            BinaryString binaryStr = BinaryString.FromString(String, Encoding.UTF8);
            BinaryString invertedBinaryStr = binaryStr.InvertBinaries();
            BinaryString reversedBinaryStr = invertedBinaryStr.ReverseBinaries();

            StringBuilder buffer = new StringBuilder();
            int index;

            foreach (char c in reversedBinaryStr.ToString())
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

                if (_random.Next(100) > 100 - InMiddleCharsStrength)
                {
                    index = _random.Next(_charsInTheMiddle.Length);
                    buffer.Append(_charsInTheMiddle[index]);
                }
            }

            return buffer.ToString();
        }

        private string Decode()
        {
            StringBuilder buffer = new StringBuilder();

            foreach (char c in String.Where(x => !_charsInTheMiddle.Contains(x)))
            {
                if (_charsForZero.Contains(c))
                {
                    buffer.Append('0');
                }
                else if (_charsForOne.Contains(c))
                {
                    buffer.Append('1');
                }
                else
                {
                    throw new ArgumentException(string.Format("Invalid character '{0}'", c));
                }
            }

            BinaryString binaryStr = BinaryString.FromBinaryString(buffer.ToString());
            BinaryString reversedBinaryStr = binaryStr.ReverseBinaries();
            BinaryString invertedBinaryStr = reversedBinaryStr.InvertBinaries();

            return invertedBinaryStr.OriginalString;
        }

        private string GetBinaryString(byte[] bytes)
        {
            return string.Join(string.Empty, bytes.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }

        private byte[] GetByteArray(string binaryStr)
        {
            int numOfBytes = binaryStr.Length / 8;
            byte[] bytes = new byte[numOfBytes];

            for (int i = 0; i < numOfBytes; ++i)
            {
                bytes[i] = Convert.ToByte(binaryStr.Substring(8 * i, 8), 2);
            }

            return bytes;
        }

        private string InvertBinaries(string binaryStr)
        {
            StringBuilder buffer = new StringBuilder();

            foreach (char c in binaryStr)
            {
                buffer.Append((c == '1') ? '0' : '1');
            }

            return buffer.ToString();
        }

        private string ReverseBinaries(string binaryStr)
        {
            char[] charArray = binaryStr.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
