using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hector.Core
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
    }
}
