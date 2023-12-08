using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Compression
{
    public class LZ77Helper
    {
        private const byte _referencePrefix = (byte)'`';
        private const int _referenceIntBase = 96;
        private const int _referenceIntFloorCode = ' ';
        private const int _referenceIntCeilCode = _referenceIntFloorCode + _referenceIntBase;
        private const int _defaultWindowLength = 9220;
        private const int _minStringLength = 5;

        private static readonly int _maxStringDistance = (int)Math.Pow(_referenceIntBase, 2) - 1;
        private static readonly int _maxStringLength = (int)Math.Pow(_referenceIntBase, 1) - 1 + _minStringLength;
        private static readonly int _maxWindowLength = _maxStringDistance + _minStringLength;

        public static byte[] CompressBytes(byte[] data, int windowLength = -1) =>
            new LZ77Helper().Compress(data, windowLength);

        public static byte[] DecompressBytes(byte[] data) =>
            new LZ77Helper().Decompress(data);

        public static string CompressString(string data, Encoding? encoding = null, int windowLength = -1)
        {
            encoding ??= Encoding.UTF8;
            byte[] inputData = encoding.GetBytes(data);
            byte[] compressed = CompressBytes(inputData, windowLength);
            return encoding.GetString(compressed);
        }

        public static string DecompressStrings(string data, Encoding? encoding)
        {
            encoding ??= Encoding.UTF8;
            byte[] inputData = encoding.GetBytes(data);
            byte[] decompressed = DecompressBytes(inputData);
            return encoding.GetString(decompressed);
        }

        private byte[] Compress(byte[] data, int windowLength)
        {
            if (windowLength == -1)
            {
                windowLength = _defaultWindowLength;
            }

            if (windowLength > _maxWindowLength)
            {
                throw new NotSupportedException("Window length is too large.");
            }

            List<byte> compressed = [];

            int pos = 0;
            int lastPos = data.Length - _minStringLength;

            while (pos < lastPos)
            {
                //Stopwatch w = Stopwatch.StartNew();

                int searchStart = Math.Max(pos - windowLength, 0);
                int matchLength = _minStringLength;
                bool foundMatch = false;
                int bestMatchDistance = _maxStringDistance;
                int bestMatchLength = 0;
                List<byte> newCompressed = [];

                while ((searchStart + matchLength) < pos)
                {
                    int sourceWindowEnd = Math.Min(searchStart + matchLength, data.Length);
                    int targetWindowEnd = Math.Min(pos + matchLength, data.Length);

                    ArraySegment<byte> s1 = new(data, searchStart, sourceWindowEnd - searchStart);
                    ArraySegment<byte> s2 = new(data, pos, targetWindowEnd - pos);

                    bool isValidMatch = s1.SequenceEqual(s2) && matchLength < _maxStringLength;

                    if (isValidMatch)
                    {
                        matchLength++;
                        foundMatch = true;
                    }
                    else
                    {
                        int realMatchLength = matchLength - 1;

                        if (foundMatch && (realMatchLength > bestMatchLength))
                        {
                            bestMatchDistance = pos - searchStart - realMatchLength;
                            bestMatchLength = realMatchLength;
                        }

                        matchLength = _minStringLength;
                        searchStart++;
                        foundMatch = false;
                    }
                }

                if (bestMatchLength != 0)
                {
                    newCompressed.Add(_referencePrefix);
                    newCompressed.AddRange(EncodeReferenceInt(bestMatchDistance, 2));
                    newCompressed.AddRange(EncodeReferenceLength(bestMatchLength));

                    pos += bestMatchLength;
                }
                else
                {
                    if (data[pos] != _referencePrefix)
                    {
                        newCompressed = new List<byte>(data[pos].AsArray());
                    }
                    else
                    {
                        newCompressed = new List<byte>(new byte[] { _referencePrefix, _referencePrefix });
                    }

                    pos++;
                }

                compressed.AddRange(newCompressed);

                //Console.WriteLine("{0}", w.ElapsedMilliseconds);
            }

            ArraySegment<byte> slice = new(data, pos, data.Length - pos);
            for (int i = 0; i < slice.Count; ++i)
            {
                if (slice[i] == _referencePrefix)
                {
                    compressed.Add(_referencePrefix);
                }

                compressed.Add(slice[i]);
            }

            return compressed.ToArray();
        }

        private byte[] Decompress(byte[] data)
        {
            List<byte> decompressed = [];
            int pos = 0;

            while (pos < data.Length)
            {
                byte currentByte = data[pos];

                if (currentByte != _referencePrefix)
                {
                    decompressed.Add(currentByte);
                    pos++;
                    continue;
                }

                byte nextByte = data[pos + 1];

                if (nextByte != _referencePrefix)
                {
                    ArraySegment<byte> s1 = new(data, pos + 1, 2);
                    ArraySegment<byte> s2 = new(data, pos + 3, 1);

                    int distance = DecodeReferenceInt(s1, 2);
                    int length = DecodeReferenceLength(s2);
                    int start = decompressed.Count - distance - length;
                    int end = start + length;

                    ArraySegment<byte> s3 = new(decompressed.ToArray(), start, end - start);
                    decompressed.AddRange(s3);
                    pos += _minStringLength - 1;
                    continue;
                }

                decompressed.Add(_referencePrefix);
                pos += 2;
            }

            return decompressed.ToArray();
        }

        private ArraySegment<byte> EncodeReferenceInt(int value, int width)
        {
            if ((value >= 0) && (value < (Math.Pow(_referenceIntBase, width) - 1)))
            {
                List<byte> encoded = [];

                while (value > 0)
                {
                    byte b = (byte)((value % _referenceIntBase) + _referenceIntFloorCode);
                    encoded.Insert(0, b);
                    value = (int)Math.Floor((double)value / _referenceIntBase);
                }

                int missingLength = width - encoded.Count;

                for (int i = 0; i < missingLength; i++)
                {
                    byte b = _referenceIntFloorCode;
                    encoded.Insert(0, b);
                }

                return new(encoded.ToArray());
            }
            else
            {
                throw new ArgumentException(string.Format("Reference int out of range: {0} (width = {1})", value, width));
            }
        }

        private ArraySegment<byte> EncodeReferenceLength(int length) =>
            EncodeReferenceInt(length - _minStringLength, 1);

        private static int DecodeReferenceInt(ArraySegment<byte> data, int width)
        {
            int value = 0;

            for (int i = 0; i < width; i++)
            {
                value *= _referenceIntBase;

                int charCode = data[i];

                if ((charCode >= _referenceIntFloorCode) && (charCode <= _referenceIntCeilCode))
                {
                    value += charCode - _referenceIntFloorCode;
                }
                else
                {
                    throw new ArgumentException("Invalid char code in reference int: " + charCode);
                }
            }

            return value;
        }

        private static int DecodeReferenceLength(ArraySegment<byte> data) =>
            DecodeReferenceInt(data, 1) + _minStringLength;
    }
}
