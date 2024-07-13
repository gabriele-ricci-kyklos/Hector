using FastMember;

namespace Hector.Data.DataReaders
{
    internal class ArrayDataReader<T> : ObjectDataReader
    {
        private readonly TypeAccessor _typeAccessor;
        private readonly T[] _items;

        private int _index = 0;
        private object? _current;

        public ArrayDataReader(T[] values)
            : base(typeof(T))
        {
            _typeAccessor = TypeAccessor.Create(Type);
            _items = values;
        }

        public override object GetValue(int i) => _typeAccessor[_current, IndexedMembers[i].Name];

        public override bool Read()
        {
            bool result = _index < _items.Length;
            _current = result ? _items[_index++] : default;
            return result;
        }
    }
}
