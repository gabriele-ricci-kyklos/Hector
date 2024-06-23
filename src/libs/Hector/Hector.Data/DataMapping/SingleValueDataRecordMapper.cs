using Hector.Core;
using System;

namespace Hector.Data.DataMapping
{
    internal class SingleValueDataRecordMapper : BaseDataRecordMapper
    {
        public override int FieldsCount => 1;

        public SingleValueDataRecordMapper(Type type, string[] dataRecordColumns)
            : base(type, dataRecordColumns)
        {
        }

        public override object? Build(int position, DataRecord[] records) =>
            records[position].Value?.ConvertTo(_type);
    }
}
