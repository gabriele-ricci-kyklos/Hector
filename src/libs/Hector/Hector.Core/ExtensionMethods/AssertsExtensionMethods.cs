using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Hector.Core
{
    public static class AssertsExtensionMethods
    {
        [return: NotNullIfNotNull(nameof(item))]
        public static T GetNonNullOrThrow<T>(this T? item, [CallerMemberName] string methodName = "")
        {
            if (item != null)
            {
                return item;
            }

            throw new ArgumentNullException(methodName);
        }

        [return: NotNullIfNotNull(nameof(item))]
        public static T GetNonNullOrThrow<T>(this T? item, Func<Exception> exFactory)
        {
            if (item != null)
            {
                return item;
            }

            throw (exFactory ?? (() => new ArgumentNullException("item is null but exFactory is also null!")))();
        }

        public static T? GetValidatedOrThrow<T>(this T? item, Func<T?, bool> validator, [CallerMemberName] string methodName = "")
        {
            if (!validator(item))
            {
                throw new ArgumentException(methodName);
            }

            return item;
        }

        public static string GetTextOrThrow(this string? item, [CallerMemberName] string methodName = "")
        {
            if (!string.IsNullOrEmpty(item))
            {
                return item;
            }

            throw new ArgumentException(methodName + " is null or empty");
        }
    }
}
