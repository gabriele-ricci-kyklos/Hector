using FastMember;
using System;
using System.Collections.Generic;

namespace Hector.Data.DataReaders
{
    public class EnumerableDataReader<T> : ObjectDataReader
    {
        private readonly TypeAccessor _typeAccessor;
        private readonly IEnumerator<T> _enumerator;

        private object? _current;

        public EnumerableDataReader(IEnumerable<T> values)
            : base(typeof(T))
        {
            _typeAccessor = TypeAccessor.Create(Type);
            _enumerator = values.GetEnumerator();
        }

        public override object GetValue(int i) => _typeAccessor[_current, IndexedMembers[i].Name];

        public override bool Read()
        {
            bool returnValue = _enumerator.MoveNext();
            _current = returnValue ? _enumerator.Current : Type.IsValueType ? Activator.CreateInstance(Type) : null;
            return returnValue;
        }
    }
}
