using System;
using System.Collections.Generic;

namespace Hector.Core.Collections
{
    public class FuncEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, int> _getHashCodeFx;
        private readonly Func<T?, T?, bool> _equalsFx;

        public static FuncEqualityComparer<T> Create(Func<T, int> getHashCodeFx, Func<T?, T?, bool> equalsFx) =>
            new(getHashCodeFx, equalsFx);

        public static FuncEqualityComparer<T> ByProperty(Func<T?, object> propertyFx, bool nullValuesEqual = true)
        {
            Func<T?, T?, bool> equalsFx = (x, y) =>
            {
                var xProp = propertyFx(x);
                var yProp = propertyFx(y);

                if (xProp is null && yProp is null)
                {
                    return nullValuesEqual;
                }

                if (xProp is null)
                {
                    return false;
                }

                return xProp.Equals(yProp);
            };

            Func<T, int> getHashCodeFx = (x) =>
            {
                var prop = propertyFx(x);
                return prop?.GetHashCode() ?? (nullValuesEqual ? 0 : x?.GetHashCode() ?? 0);
            };

            return Create(getHashCodeFx, equalsFx);
        }

        private FuncEqualityComparer(Func<T, int> getHashCodeFx, Func<T?, T?, bool> equalsFx)
        {
            _getHashCodeFx = getHashCodeFx;
            _equalsFx = equalsFx;
        }

        public bool Equals(T? x, T? y) => _equalsFx(x, y);

        public int GetHashCode(T obj) => _getHashCodeFx(obj);
    }
}
