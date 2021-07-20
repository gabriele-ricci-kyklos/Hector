using System;
using System.Collections.Generic;

namespace Hector.Core.Support.Collections.Comparers.Equality
{
    public class LinqEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, int> _getHashCodeFx;
        private readonly Func<T, T, bool> _equalsFx;

        public static LinqEqualityComparer<T> Create(Func<T, int> getHashCodeFx, Func<T, T, bool> equalsFx)
        {
            return new LinqEqualityComparer<T>(getHashCodeFx, equalsFx);
        }

        private LinqEqualityComparer(Func<T, int> getHashCodeFx, Func<T, T, bool> equalsFx)
        {
            _getHashCodeFx = getHashCodeFx;
            _equalsFx = equalsFx;
        }

        public bool Equals(T x, T y) => _equalsFx(x, y);

        public int GetHashCode(T obj) => _getHashCodeFx(obj);
    }
}
