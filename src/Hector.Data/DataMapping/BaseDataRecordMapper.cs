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
        protected DataRecordMapperFactory _mapperFactory;

        public abstract int FieldsCount { get; }

        public abstract object? Build(int position, DataRecord[] records);

        protected BaseDataRecordMapper(Type type, DataRecordMapperFactory mapperFactory)
        {
            _type = type;
            _mapperFactory = mapperFactory;
        }
    }
}
