using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Hector.Core.Support
{
    public static class StringsExtensionMethods
    {
        public static bool IsNullOrBlankString(this string str)
        {
            return str.IsNull() || str == string.Empty;
        }

        public static string ToSafeString(this object item, string nullValueReplacement = "")
        {
            if (item == null)
            {
                return nullValueReplacement;
            }

            return item.ToString();
        }

        public static string FormatWith(this string format, params object[] args)
        {
            return format.IsNullOrEmpty() ? string.Empty : string.Format(format, args);
        }

        public static string StringAppend(this string str, string str2, string separator = "")
        {
            if (separator.IsNull())
            {
                return "{0}{1}".FormatWith(str, str2);
            }

            return "{0}{1}{2}".FormatWith(str, separator, str2);
        }

        public static string StringJoin(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values);
        }

        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static string ToEmptyIfNull(this string s)
        {
            return s.IsNullOrEmpty() ? string.Empty : s;
        }

        public static string ReplaceInsensitive(this string s, string oldText, string newText)
        {
            if (s.IsNull())
            {
                return null;
            }
            return Regex.Replace(s, oldText, newText, RegexOptions.IgnoreCase);
        }

        public static string ToNullIfEmpty(this string s)
        {
            return s.IsNullOrEmpty() ? null : s;
        }

        public static string SafeGetLeftPart(this string s, int n)
        {
            if (s.IsNullOrEmpty())
            {
                return string.Empty;
            }
            if (n >= s.Length)
            {
                return s;
            }
            if (n < 0)
            {
                return s;
            }
            return s.Substring(0, n);
        }

        public static string SafeGetRightPart(this string s, int n)
        {
            if (s.IsNullOrEmpty())
            {
                return string.Empty;
            }
            if (n >= s.Length)
            {
                return s;
            }
            if (n < 0)
            {
                return s;
            }

            return s.Substring(s.Length - n);
        }

        public static string Shuffle(this string str)
        {
            char[] strCh = str.ToCharArray();
            strCh.Shuffle();
            return new string(strCh);
        }
    }
}
