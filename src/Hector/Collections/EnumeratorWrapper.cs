using System;
using System.Collections.Generic;
using System.Linq;

namespace Hector.Collections
{
    public class EnumeratorWrapper<T> : IDisposable
    {
        private readonly IEnumerator<T> _enumerator;

        public T NextValue => GetRangeValues(1).First();
        public T? SafeNextValue => GetRangeValues(1).FirstOrDefault();
        public T Current => _enumerator.Current;

        public EnumeratorWrapper(IEnumerable<T> items)
            : this(items.GetEnumerator())
        {
        }

        public EnumeratorWrapper(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public IEnumerable<T> GetRangeValues(int rangeSize)
        {
            int i = 0;
            while (i++ < rangeSize && _enumerator.MoveNext())
            {
                yield return _enumerator.Current;
            }
        }

        public void Dispose() => _enumerator.Dispose();
    }
}
