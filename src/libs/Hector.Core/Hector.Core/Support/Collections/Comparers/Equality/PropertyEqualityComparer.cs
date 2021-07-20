using System;
using System.Collections.Generic;

namespace Hector.Core.Support.Collections.Comparers.Equality
{
    public class PropertyEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _eqComparer;
        private readonly Func<T, int> _hashFunction;

        public static IEqualityComparer<T> ByProperty(Func<T, object> propertyFx, bool nullValuesEqual = true)
        {
            return new PropertyEqualityComparer<T>(propertyFx, nullValuesEqual);
        }

        private PropertyEqualityComparer(Func<T, object> propertyFx, bool nullValuesEqual = true)
        {
            _eqComparer = (x, y) =>
            {
                var xProp = propertyFx(x);
                var yProp = propertyFx(y);

                if (xProp.IsNull() && yProp.IsNull())
                {
                    return nullValuesEqual;
                }

                if (xProp.IsNull())
                {
                    return false;
                }

                return xProp.Equals(yProp);
            };

            _hashFunction = (x) => propertyFx(x).Return(z => z.GetHashCode(), nullValuesEqual ? 0 : x.GetHashCode());
        }

        public bool Equals(T x, T y) => _eqComparer(x, y);

        public int GetHashCode(T obj) => _hashFunction(obj);
    }
}
