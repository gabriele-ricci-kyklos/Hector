using FastMember;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Hector.Data.DataReaders
{
    public class EnumerableDataReader<T> : EnumerableDataReader, IEnumerable<T>
    {
        protected readonly IEnumerator<T> _typedEnumerator;

        public EnumerableDataReader(IEnumerable<T> values)
            : base(typeof(T), values)
        {
            _typedEnumerator = values.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _typedEnumerator;

        public override void Dispose()
        {
            base.Dispose();
            _typedEnumerator.Dispose();
        }
    }

    public class EnumerableDataReader : ObjectDataReader, IEnumerable
    {
        protected readonly TypeAccessor _typeAccessor;
        protected readonly IEnumerator _enumerator;

        protected object? _current;

        public EnumerableDataReader(Type type, IEnumerable values)
            : base(type)
        {
            _typeAccessor = TypeAccessor.Create(_type);
            _enumerator = values.GetEnumerator();
        }

        public IEnumerator GetEnumerator() => _enumerator;

        public override object GetValue(int i) => _typeAccessor[_current, _indexedMembers[i].Name];

        public override bool Read()
        {
            bool returnValue = _enumerator.MoveNext();
            _current = returnValue ? _enumerator.Current : _type.IsValueType ? Activator.CreateInstance(_type) : null;
            return returnValue;
        }
    }
}
