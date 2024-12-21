namespace Hector.Data.DataReaders.Db
{
    public class ArrayDbDataReader<T>(T[] values) : EnumerableDbDataReader<T>(values)
    {
        private int _index = 0;
        private readonly T[] _items = values;

        public override object GetValue(int i) => _typeAccessor[_currentObject, _indexedMembers[i].Name];

        public override bool Read()
        {
            bool result = _index < _items.Length;
            _currentObject = result ? _items[_index++] : default;
            return result;
        }
    }
}
