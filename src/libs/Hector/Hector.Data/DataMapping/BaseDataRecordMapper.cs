using System;

namespace Hector.Data.DataMapping
{
    internal interface IDataRecordMapper
    {
        int FieldsCount { get; }
        object? Build(int position, DataRecord[] records);
    }

    internal abstract class BaseDataRecordMapper : IDataRecordMapper
    {
        protected Type _type;
        protected string[] _dataRecordColumns = [];

        public abstract int FieldsCount { get; }

        public abstract object? Build(int position, DataRecord[] records);

        protected BaseDataRecordMapper(Type type, string[] dataRecordColumns)
        {
            _type = type;
            _dataRecordColumns = dataRecordColumns;
        }
    }
}
