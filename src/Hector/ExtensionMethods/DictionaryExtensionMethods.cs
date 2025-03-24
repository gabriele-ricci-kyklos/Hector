using System;
using System.Collections.Generic;
using System.Linq;

namespace Hector
{
    public static class DictionaryExtensionMethods
    {
        public static TValue[] GetValues<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<TKey> keyList) =>
            keyList
                .ToEmptyIfNull()
                .Where(dict.ContainsKey)
                .Select(k => dict[k])
                .ToArray();

        internal enum DictMergeStrategy { Override, Keep }

        public static Dictionary<K, V> MergeLeft<K, V>(this Dictionary<K, V> me, params Dictionary<K, V>[] others)
            where K : notnull =>
            me.Merge(DictMergeStrategy.Keep, others);

        public static Dictionary<K, V> MergeRight<K, V>(this Dictionary<K, V> me, params Dictionary<K, V>[] others)
            where K : notnull =>
            me.Merge(DictMergeStrategy.Override, others);

        private static Dictionary<K, V> Merge<K, V>(this Dictionary<K, V> me, DictMergeStrategy mergeStrategy, params Dictionary<K, V>[] others)
            where K : notnull
        {
            int capacity = me.Count + others.Sum(x => x.Count);
            Dictionary<K, V> newMap = new(me, me.Comparer);

            var allData =
                others
                    .SelectMany(x => x);

            foreach (KeyValuePair<K, V> p in allData)
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

            return newMap;
        }
    }
}
