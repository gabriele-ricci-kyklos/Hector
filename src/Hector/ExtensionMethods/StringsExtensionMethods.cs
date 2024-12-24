using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hector
{
    public static class StringsExtensionMethods
    {
        public static bool IsNullOrBlankString([NotNullWhen(false)] this string? s) => string.IsNullOrWhiteSpace(s);
        public static bool IsNotNullAndNotBlank([NotNullWhen(true)] this string? s) => !string.IsNullOrWhiteSpace(s);
        public static string StringJoin<T>(this IEnumerable<T> values, string separator) => string.Join(separator, values);
        public static string StringJoin<T>(this IEnumerable<T> values, char separator) => string.Join(separator, values);
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
            if (startPos > s.Length)
            {
                return string.Empty;
            }
            if (startPos + len >= s.Length)
            {
                return s.Substring(startPos);
            }

            return s.Substring(startPos, len);
        }
    }
}
