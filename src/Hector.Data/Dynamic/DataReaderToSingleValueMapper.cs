using System;
using System.Data;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal class DataReaderToSingleValueMapper : IDataReaderToEntityMapper
    {
        public int FieldPosition { get; }
        public Type FieldType { get; }

        public DataReaderToSingleValueMapper(int fieldPosition, Type fieldType)
        {
            if(fieldPosition < 0) throw new ArgumentOutOfRangeException(nameof(fieldPosition));
            FieldPosition = fieldPosition;
            FieldType = fieldType;
        }

        public ValueTask<object> BuildAsync(IDataRecord dataRecord, int _)
        {
            object value = dataRecord.GetValue(FieldPosition);
            if (value is not null && value != DBNull.Value)
            {
                value = Convert.ChangeType(value, FieldType);
            }
            return new ValueTask<object>(value!);
        }
    }
}
