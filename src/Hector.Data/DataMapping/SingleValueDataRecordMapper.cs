using System;

namespace Hector.Data.DataMapping
{
    internal class SingleValueDataRecordMapper : BaseDataRecordMapper
    {
        public override int FieldsCount => 1;

        public SingleValueDataRecordMapper(Type type, DataRecordMapperFactory mapperFactory)
            : base(type, mapperFactory)
        {
        }

        public override object? Build(int position, DataRecord[] records) =>
            records[position].Value?.ConvertTo(_type);
    }
}
