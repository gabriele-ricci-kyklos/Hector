using System.Collections.Generic;
using System.Linq;

namespace Hector.Core.Collections
{
    public class Bictionary<TKey, TValue>(IDictionary<TKey, TValue> data)
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _forward = new(data);
        private readonly Dictionary<TValue, TKey> _reverse = data.ToDictionary(x => x.Value, x => x.Key);

        public Bictionary(IEnumerable<(TKey, TValue)> data)
            : this(data.ToEmptyIfNull().ToDictionary(x => x.Item1, x => x.Item2))
        {
        }

        public TValue? GetForwardValueOrDefault(TKey key, TValue defaultValue = default!) => _forward.GetValueOrDefault(key, defaultValue);
        public TKey? GetReverseValueOrDefault(TValue key, TKey defaultValue = default!) => _reverse.GetValueOrDefault(key, defaultValue);
    }
}
