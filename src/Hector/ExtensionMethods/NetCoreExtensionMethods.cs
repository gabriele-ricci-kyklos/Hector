using System;
using System.Collections.Generic;

namespace Hector
{
    internal static class NetCoreExtensionMethods
    {
        internal static TValue? NetCoreGetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
#if NET5_0_OR_GREATER
            return dictionary.GetValueOrDefault(key);
#else
            return dictionary!.NetCoreGetValueOrDefault(key, default);
#endif
        }

        internal static TValue NetCoreGetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
#if NET5_0_OR_GREATER
            return dictionary.GetValueOrDefault(key, defaultValue);
#else
            if (dictionary?.TryGetValue(key, out TValue value) ?? false)
            {
                return value;
            }

            return defaultValue;
#endif
        }

        internal static bool NetCoreTryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
#if NET5_0_OR_GREATER
            return dictionary.TryAdd(key, value);
#else
            if (dictionary is null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }

            return false;
#endif
        }

        internal static T GetItemByIndex<T>(this ArraySegment<T> segment, int index)
        {
#if NET5_0_OR_GREATER
            return segment[index];
#else
            return GetItemByIndex<ArraySegment<T>, T>(segment, index);
#endif
        }

        private static T GetItemByIndex<TList, T>(TList list, int index) where TList : struct, IReadOnlyList<T> =>
            list[index];
    }
}
