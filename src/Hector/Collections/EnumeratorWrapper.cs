﻿using System;
using System.Collections.Generic;

namespace Hector.Collections
{
    public sealed class EnumeratorWrapper<T> : IDisposable
    {
        private readonly IEnumerator<T> _enumerator;

        public T NextValue
        {
            get
            {
                if (_enumerator.MoveNext())
                {
                    return _enumerator.Current;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public T? SafeNextValue
        {
            get
            {
                if (_enumerator.MoveNext())
                {
                    return _enumerator.Current;
                }

                return default;
            }
        }

        public T Current => _enumerator.Current;

        public EnumeratorWrapper(IEnumerable<T> items)
            : this(items.GetEnumerator())
        {
        }

        public EnumeratorWrapper(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public IEnumerable<T> EnumerateRangeValues(int rangeSize)
        {
            int i = 0;
            while (i++ < rangeSize && _enumerator.MoveNext())
            {
                yield return _enumerator.Current;
            }
        }

        public T[] GetRangeValues(int rangeSize)
        {
            T[] buffer = new T[rangeSize];
            int i;

            for (i = 0; i < rangeSize; ++i)
            {
                if (!_enumerator.MoveNext())
                {
                    break;
                }

                buffer[i] = _enumerator.Current;
            }

            if (i != rangeSize - 1)
            {
                T[] newBuffer = new T[i];
                Array.Copy(buffer, newBuffer, newBuffer.Length);
                return newBuffer;
            }

            return buffer;
        }

        public void Dispose() => _enumerator.Dispose();
    }
}
