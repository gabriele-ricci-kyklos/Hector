using System;
using System.Collections.Generic;
using System.Linq;

namespace Hector.Core.Support.Collections.Enumerators
{
    public class EnumeratorWrapperDictionary<TKey, TValue> : IDisposable
    {
        private readonly IDictionary<TKey, EnumeratorWrapper<TValue>> _dict;

        private EnumeratorWrapperDictionary()
        {
            _dict = new Dictionary<TKey, EnumeratorWrapper<TValue>>();
        }

        public EnumeratorWrapperDictionary(IDictionary<TKey, IEnumerable<TValue>> data)
            : this()
        {
            IEnumerable<Tuple<TKey, IEnumerable<TValue>>> TupleList =
                data
                    .Select(x => new Tuple<TKey, IEnumerable<TValue>>(x.Key, x.Value));

            Initialize(TupleList);
        }

        public EnumeratorWrapperDictionary(IDictionary<TKey, IList<TValue>> data)
            : this()
        {
            IEnumerable<Tuple<TKey, IEnumerable<TValue>>> TupleList =
                data
                    .Select(x => new Tuple<TKey, IEnumerable<TValue>>(x.Key, x.Value));

            Initialize(TupleList);
        }

        public EnumeratorWrapperDictionary(IDictionary<TKey, TValue[]> data)
            : this()
        {
            IEnumerable<Tuple<TKey, IEnumerable<TValue>>> TupleList =
                data
                    .Select(x => new Tuple<TKey, IEnumerable<TValue>>(x.Key, x.Value));

            Initialize(TupleList);
        }

        private void Initialize(IEnumerable<Tuple<TKey, IEnumerable<TValue>>> data)
        {
            data.AssertNotNull(nameof(data));

            foreach (var item in data)
            {
                _dict.Add(item.Item1, new EnumeratorWrapper<TValue>(item.Item2.ToEmptyIfNull()));
            }
        }

        public TValue GetNextValueByKey(TKey key)
        {
            EnumeratorWrapper<TValue> wrapper = _dict.GetValueOrDefault(key, null);

            if (wrapper.IsNull())
            {
                throw new KeyNotFoundException($"No values found for the key {key}");
            }

            return wrapper.NextValue;
        }

        public void Dispose()
        {
            _dict
                .Values
                .ForEach(x => x?.Dispose());
        }
    }
}
