using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Hector
{
    public static class StringsExtensionMethods
    {
        public static bool IsNullOrBlankString([NotNullWhen(false)] this string? s) => string.IsNullOrWhiteSpace(s);
        public static bool IsNotNullAndNotBlank([NotNullWhen(true)] this string? s) => !string.IsNullOrWhiteSpace(s);
        public static string StringJoin<T>(this IEnumerable<T> values, string separator) => string.Join(separator, values);
        public static string Shuffle(this string str)
        {
            char[] strCh = str.ToCharArray();
            strCh.Shuffle();
            return new string(strCh);
        }

        public static string? ToNullIfBlank(this string? s)
        {
            if (s.IsNotNullAndNotBlank())
            {
                return s;
            }

            return null;
        }

        public static string ToHexString(this byte[] bytes, bool toLower = true)
        {
            string s = BitConverter.ToString(bytes).Replace("-", "");
            if (!toLower)
            {
                return s;
            }

            return s.ToLowerInvariant();
        }

        public static string SafeSubstring(this string? s, int startPos, int len)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            if (len < 0)
            {
                return string.Empty;
            }
            if (startPos < 0)
            {
                startPos = 0;
            }
            if (startPos > s!.Length)
            {
                return string.Empty;
            }
            if (startPos + len >= s.Length)
            {
                return s.Substring(startPos);
            }

            return s.Substring(startPos, len);
        }

        public static T? ToNumber<T>(this string? s, IFormatProvider? formatProvider = null, NumberStyles numberStyles = NumberStyles.Any) where T : struct =>
            (T?)ToNumber(s, typeof(T), formatProvider, numberStyles);

        public static object? ToNumber(this string? s, Type type, IFormatProvider? formatProvider = null, NumberStyles numberStyles = NumberStyles.Any)
        {
            static bool isNumericType(Type? type) => (uint)(Type.GetTypeCode(type) - 4) <= 11u;

            if (s.IsNullOrBlankString()
                || (type is null || !isNumericType(type)))
            {
                return null;
            }

            formatProvider ??= CultureInfo.InvariantCulture;

            s = s!.Trim();

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => byte.TryParse(s, numberStyles, formatProvider, out byte value) ? value.ConvertTo(type) : null,
                TypeCode.Decimal => decimal.TryParse(s, numberStyles, formatProvider, out decimal value) ? value.ConvertTo(type) : null,
                TypeCode.Double => double.TryParse(s, numberStyles, formatProvider, out double value) ? value.ConvertTo(type) : null,
                TypeCode.Int16 => short.TryParse(s, numberStyles, formatProvider, out short value) ? value.ConvertTo(type) : null,
                TypeCode.Int32 => int.TryParse(s, numberStyles, formatProvider, out int value) ? value.ConvertTo(type) : null,
                TypeCode.Int64 => long.TryParse(s, numberStyles, formatProvider, out long value) ? value.ConvertTo(type) : null,
                TypeCode.SByte => sbyte.TryParse(s, numberStyles, formatProvider, out sbyte value) ? value.ConvertTo(type) : null,
                TypeCode.Single => float.TryParse(s, numberStyles, formatProvider, out float value) ? value.ConvertTo(type) : null,
                TypeCode.UInt16 => ushort.TryParse(s, numberStyles, formatProvider, out ushort value) ? value.ConvertTo(type) : null,
                TypeCode.UInt32 => uint.TryParse(s, numberStyles, formatProvider, out uint value) ? value.ConvertTo(type) : null,
                TypeCode.UInt64 => ulong.TryParse(s, numberStyles, formatProvider, out ulong value) ? value.ConvertTo(type) : null,
                _ => null
            };
        }

        public static async Task WriteStreamAsync(this string str, Stream stream, Encoding? encoding = null, int bufferSize = 4096)
        {
            using StreamWriter writer = new(stream, encoding ?? Encoding.UTF8, bufferSize, true);
            await writer.WriteAsync(str).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            stream.Position = 0;
        }
    }
}
