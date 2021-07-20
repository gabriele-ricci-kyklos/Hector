using Hector.Core.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Core.Compression
{
    public class LZ77
    {
        static byte _referencePrefix = (byte)'`';
        static int _referenceIntBase = 96;
        static int _referenceIntFloorCode = ' ';
        static int _referenceIntCeilCode = _referenceIntFloorCode + _referenceIntBase;
        static int _maxStringDistance = (int)Math.Pow(_referenceIntBase, 2) - 1;
        static int _minStringLength = 5;
        static int _maxStringLength = (int)Math.Pow(_referenceIntBase, 1) - 1 + _minStringLength;
        static int _defaultWindowLength = 9220;
        static int _maxWindowLength = _maxStringDistance + _minStringLength;

        public static byte[] CompressBytes(byte[] data, int windowLength = -1)
        {
            LZ77 lz = new LZ77();
            return lz.Compress(data, windowLength);
        }

        public static byte[] DecompressBytes(byte[] data)
        {
            LZ77 lz = new LZ77();
            return lz.Decompress(data);
        }

        public static string CompressStrings(string data, Encoding encoding = null, int windowLength = -1)
        {
            data.AssertHasText(nameof(data));
            encoding = encoding ?? Encoding.UTF8;
            byte[] inputData = encoding.GetBytes(data);

            LZ77 lz = new LZ77();
            byte[] compressed = lz.Compress(inputData, windowLength);
            return encoding.GetString(compressed);
        }

        public static string DecompressStrings(string data, Encoding encoding)
        {
            data.AssertHasText(nameof(data));
            encoding = encoding ?? Encoding.UTF8;
            byte[] inputData = encoding.GetBytes(data);

            LZ77 lz = new LZ77();
            byte[] decompressed = lz.Decompress(inputData);
            return encoding.GetString(decompressed);
        }

        public byte[] Compress(byte[] data)
        {
            return Compress(data, -1);
        }

        public byte[] Compress(byte[] data, int windowLength)
        {
            if (windowLength == -1)
            {
                windowLength = _defaultWindowLength;
            }

            if (windowLength > _maxWindowLength)
            {
                throw new ArgumentException("Window length is too large.");
            }

            List<byte> compressed = new List<byte>();

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
                List<byte> newCompressed = new List<byte>();

                while ((searchStart + matchLength) < pos)
                {
                    int sourceWindowEnd = Math.Min(searchStart + matchLength, data.Length);
                    int targetWindowEnd = Math.Min(pos + matchLength, data.Length);

                    var s1 = new ArraySegment<byte>(data, searchStart, sourceWindowEnd - searchStart) as IList<byte>;
                    var s2 = new ArraySegment<byte>(data, pos, targetWindowEnd - pos) as IList<byte>;

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

            var slice = new ArraySegment<byte>(data, pos, data.Length - pos) as IList<byte>;
            for(int i = 0; i < slice.Count; ++i)
            {
                if(slice[i] == _referencePrefix)
                {
                    compressed.Add(_referencePrefix);
                }

                compressed.Add(slice[i]);
            }

            return compressed.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            List<byte> decompressed = new List<byte>();
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
                    var s1 = new ArraySegment<byte>(data, pos + 1, 2) as IList<byte>;
                    var s2 = new ArraySegment<byte>(data, pos + 3, 1) as IList<byte>;

                    int distance = DecodeReferenceInt(s1, 2);
                    int length = DecodeReferenceLength(s2);
                    int start = decompressed.Count - distance - length;
                    int end = start + length;

                    var s3 = new ArraySegment<byte>(decompressed.ToArray(), start, end - start) as IList<byte>;
                    decompressed.AddRange(s3);
                    pos += _minStringLength - 1;
                    continue;
                }

                decompressed.Add(_referencePrefix);
                pos += 2;
            }

            return decompressed.ToArray();
        }

        private IList<byte> EncodeReferenceInt(int value, int width)
        {
            if ((value >= 0) && (value < (Math.Pow(_referenceIntBase, width) - 1)))
            {
                IList<byte> encoded = new List<byte>();

                while (value > 0)
                {
                    byte b = (byte)((value % _referenceIntBase) + _referenceIntFloorCode);
                    encoded.Insert(0, b);
                    value = (int)Math.Floor((double)value / _referenceIntBase);
                }

                int missingLength = width - encoded.Count;

                for (int i = 0; i < missingLength; i++)
                {
                    byte b = (byte)_referenceIntFloorCode;
                    encoded.Insert(0, b);
                }

                return encoded;
            }
            else
            {
                throw new ArgumentException(string.Format("Reference int out of range: {0} (width = {1})", value, width));
            }
        }

        private IList<byte> EncodeReferenceLength(int length)
        {
            return EncodeReferenceInt(length - _minStringLength, 1);
        }

        private int DecodeReferenceInt(IList<byte> data, int width)
        {
            int value = 0;

            for (int i = 0; i < width; i++)
            {
                value *= _referenceIntBase;

                int charCode = (int)data[i];

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

        private int DecodeReferenceLength(IList<byte> data)
        {
            return DecodeReferenceInt(data, 1) + _minStringLength;
        }
    }
}
