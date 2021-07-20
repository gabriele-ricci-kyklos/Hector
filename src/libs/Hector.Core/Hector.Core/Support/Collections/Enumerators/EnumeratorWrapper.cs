using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hector.Core.Support.Collections.Enumerators
{
    public class EnumeratorWrapper<T> : IEnumerator<T>, IEnumerator, IDisposable
    {
        private readonly IEnumerator<T> _enumerator;

        public T NextValue => GetRangeValues(1).First();
        public T Current => _enumerator.Current;
        object IEnumerator.Current => Current;

        public EnumeratorWrapper(IEnumerable<T> items)
        {
            _enumerator = items.GetEnumerator();
        }

        public EnumeratorWrapper(IEnumerator<T> enumerator)
        {
            enumerator.AssertNotNull(nameof(enumerator));
            _enumerator = enumerator;
        }

        public IEnumerable<T> GetRangeValues(int rangeSize)
        {
            for (int i = 0; i < rangeSize; ++i)
            {
                T value;

                try
                {
                    _enumerator.MoveNext();
                    value = _enumerator.Current;
                }
                catch (InvalidOperationException)
                {
                    if (i == 0)
                    {
                        throw;
                    }

                    break;
                }

                yield return value;
            }
        }

        public void Dispose() => _enumerator?.Dispose();

        public bool MoveNext() => _enumerator?.MoveNext() ?? false;

        public void Reset() => _enumerator?.Reset();
    }
}
