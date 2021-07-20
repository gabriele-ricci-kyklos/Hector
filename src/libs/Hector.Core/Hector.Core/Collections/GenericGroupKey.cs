using Hector.Core.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hector.Core.Collections
{
    public abstract class GenericGroupKey
    {
        protected bool IgnoreNullValues { get; }

        public GenericGroupKey(bool ignoreNullValues)
        {
            IgnoreNullValues = ignoreNullValues;
        }

        public GenericGroupKey()
        {
            IgnoreNullValues = true;
        }

        public override int GetHashCode()
        {
            var props = this.GetPropertyValues();

            if (!props.Any())
            {
                return base.GetHashCode();
            }

            int hashCode = IgnoreNullValues ? 0 : base.GetHashCode();
            foreach (NameValue<string, object> prop in props)
            {
                hashCode ^= prop.Value.Return(x => x.GetHashCode(), IgnoreNullValues ? 0 : base.GetHashCode() * 17);
            }

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null))
            {
                return false;
            }

            GenericGroupKey key = obj as GenericGroupKey;
            if (object.ReferenceEquals(key, null))
            {
                return false;
            }

            var thisProps = this.GetPropertyValues();
            var keyProps = key.GetPropertyValues();

            if (!thisProps.Any() || !keyProps.Any())
            {
                return base.Equals(key);
            }

            var join =
                thisProps
                    .Join
                    (
                        keyProps,
                        x => x.Name,
                        x => x.Name,
                        (x, y) => y.Value.IsNull() || y.Value.Equals(x.Value)
                    );

            return join.All(x => x);
        }

        public static bool operator ==(GenericGroupKey a, GenericGroupKey b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GenericGroupKey a, GenericGroupKey b)
        {
            return !a.Equals(b);
        }
    }
}
