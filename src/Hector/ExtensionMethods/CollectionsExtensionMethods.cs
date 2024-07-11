using System;
using System.Collections.Generic;
using System.Linq;

namespace Hector.Core
{
    public static class CollectionsExtensionMethods
    {
        public static bool IsNullOrEmptyList<T>(this IEnumerable<T>? list) =>
            list is null || !list.Any();

        public static bool IsNotNullAndNotEmptyList<T>(this IEnumerable<T>? list) =>
            list is not null && list.Any();

        public static IEnumerable<T> ToEmptyIfNull<T>(this IEnumerable<T>? enumerable) =>
            enumerable ?? [];

        public static IEnumerable<T>? ToNullIfEmpty<T>(this IEnumerable<T>? list) =>
            list.IsNullOrEmptyList() ? null : list.ToEmptyIfNull();

        public static List<T> ToEmptyListIfNull<T>(this IEnumerable<T>? enumerable) =>
            enumerable.ToEmptyIfNull().ToList();

        public static T[] ToEmptyArrayIfNull<T>(this IEnumerable<T>? enumerable) =>
            enumerable?.ToArray() ?? [];

        public static T[]? ToNullIfEmptyArray<T>(this IEnumerable<T>? list) =>
            list.IsNullOrEmptyList() ? null : list.ToEmptyArrayIfNull();

        public static List<T>? ToNullIfEmptyList<T>(this IEnumerable<T>? list) =>
            list.IsNullOrEmptyList() ? null : list.ToEmptyListIfNull();

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            if (sequence.IsNullOrEmptyList())
            {
                return;
            }

            foreach (T item in sequence)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T, int> action)
        {
            if (sequence.IsNullOrEmptyList())
            {
                return;
            }

            int index = 0;
            foreach (T item in sequence)
            {
                action(item, index);
                ++index;
            }
        }

        public static bool IsIn<T>(this T item, params T[] list) =>
            item.IsIn((IEnumerable<T>)list);

        public static bool IsIn<T>(this T item, Func<T?, T?, bool> eqFx, params T[] list) =>
            item.IsIn(list, eqFx);

        public static bool IsIn<T>(this T item, IEnumerable<T> list) =>
            item.IsIn(list, (T x, T y) => x is not null && x.Equals(y));

        public static bool IsIn<T>(this T item, IEnumerable<T> list, Func<T, T, bool> eqFx) =>
            list.ToEmptyIfNull().Any(x => eqFx(item, x));

        public static List<T> AsList<T>(this T s) => [s];

        public static T[] AsArray<T>(this T s) => [s];

        public static T[] AsArray<T>(this T item, int size = 1, bool setValueInAll = false)
        {
            T[] array = new T[size];
            array[0] = item;
            for (int i = 1; i < size && setValueInAll; ++i)
            {
                array[i] = item;
            }
            return array;
        }

        public static T[]? AsArrayOrNull<T>(this T item) => item?.AsArray();

        public static List<T>? AsListOrNull<T>(this T item) => item?.AsList();

        //credits: https://stackoverflow.com/a/24648788/4499267
        public static void Shuffle<T>(this T[] list, Random? random = null)
        {
            Random rng = random ?? new Random();

            int n = list.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        public static IEnumerable<T[]> Split<T>(this IEnumerable<T> list, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new FormatException("The size cannot be <= 0");
            }

            T[] fullList = list.ToArray();
            int count = fullList.Length;
            for (int i = 0; i < count; i += chunkSize)
            {
                yield return fullList.GetRange(i, Math.Min(chunkSize, count - i));
            }
        }

        public static T[] GetRange<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public enum DictMergeStrategy { Override, Keep }

        public static Dictionary<K, V> MergeLeft<K, V>(this Dictionary<K, V> me, params Dictionary<K, V>[] others)
            where K : notnull =>
            me.Merge(DictMergeStrategy.Keep, others);

        public static Dictionary<K, V> MergeRight<K, V>(this Dictionary<K, V> me, params Dictionary<K, V>[] others)
            where K : notnull =>
            me.Merge(DictMergeStrategy.Override, others);

        public static Dictionary<K, V> Merge<K, V>(this Dictionary<K, V> me, DictMergeStrategy mergeStrategy, params Dictionary<K, V>[] others)
            where K : notnull
        {
            int capacity = me.Count + others.Sum(x => x.Count);
            Dictionary<K, V> newMap = new(capacity, me.Comparer);

            foreach (Dictionary<K, V> src in others)
            {
                foreach (KeyValuePair<K, V> p in src)
                {
                    if (!me.TryGetValue(p.Key, out V? value))
                    {
                        newMap[p.Key] = p.Value;
                    }
                    else
                    {
                        newMap[p.Key] = mergeStrategy switch
                        {
                            DictMergeStrategy.Override => p.Value,
                            DictMergeStrategy.Keep => value,
                            _ => throw new NotSupportedException()
                        };
                    }
                }
            };

            return newMap;
        }
    }
}
