using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Core.Support
{
    public static class AssertsExtensionMethods
    {
        public static void AssertIsTrue(this bool expression, string message)
        {
            if (!expression)
            {
                throw new ArgumentException(message);
            }
        }

        public static void AssertIsFalse(this bool expression, string message)
        {
            if (expression)
            {
                throw new ArgumentException(message);
            }
        }

        public static void AssertNotNull(this object o, string varName)
        {
            if (o.IsNull())
            {
                throw new ArgumentNullException(varName);
            }
        }

        public static void AssertHasText(this string argument, string name, string message = null)
        {
            if (argument.IsNullOrEmpty())
            {
                throw new ArgumentNullException(name, $"Argument '{name}' cannot be null or resolve to an empty string : '{argument}'");
            }
        }

        public static void AssertNotNullAndHasElementsNotNull<T>(this IEnumerable<T> argument, string name, string message = null)
        {
            message = message ?? $"Argument '{name}' must not be null or resolve to an empty collection and must contain non-null elements";

            if (argument.IsNullOrEmptyList() || argument.Any(x => x.IsNull()))
            {
                throw new ArgumentException(name, message);
            }
        }

        public static void AssertHasElementsNotNull<T>(this IEnumerable<T> argument, string name, string message = null)
        {
            message = message ?? $"Argument '{name}' must not be null or resolve to an empty collection and must contain non-null elements";

            if (argument.Any(x => x.IsNull()))
            {
                throw new ArgumentException(name, message);
            }
        }

        public static void AssertNotNullAndHasElements<T>(this IEnumerable<T> argument, string name, string message = null)
        {
            message = message ?? $"Argument '{name}' must not be null or resolve to an empty collection and must contain non-null elements";

            if (argument.IsNullOrEmptyList())
            {
                throw new ArgumentException(name, message);
            }
        }

        public static void AssertHasElements<T>(this IEnumerable<T> argument, string name, string message = null)
        {
            message = message ?? $"Argument '{name}' must not be null or resolve to an empty collection and must contain non-null elements";

            if (!argument.Any())
            {
                throw new ArgumentException(name, message);
            }
        }
    }
}
