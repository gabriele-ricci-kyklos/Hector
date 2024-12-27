using System;
using System.Collections.Generic;

namespace Hector
{
    public static class NetCoreExtensionMethods
    {
#if NETSTANDARD2_0

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) =>
            new(source, comparer);

        public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
            dictionary.GetValueOrDefault(key, default!);

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) =>
            dictionary
                .GetNonNullOrThrow(nameof(dictionary))
                .TryGetValue(key, out TValue? value)
            ? value
            : defaultValue;

        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
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
        }

        public static T GetItemByIndex<T>(this ArraySegment<T> segment, int index) =>
            GetItemByIndex<ArraySegment<T>, T>(segment, index);

        private static T GetItemByIndex<TList, T>(TList list, int index) where TList : struct, IReadOnlyList<T> =>
            list[index];

#endif
    }
}
