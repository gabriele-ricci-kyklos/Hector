using System.Collections.Generic;
using System.Linq;

namespace Hector.Core
{
    public static class DictionaryExtensionMethods
    {
        public static TValue[] GetValues<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<TKey> keyList) =>
            keyList
                .ToEmptyIfNull()
                .Where(dict.ContainsKey)
                .Select(k => dict[k])
                .ToArray();
    }
}
