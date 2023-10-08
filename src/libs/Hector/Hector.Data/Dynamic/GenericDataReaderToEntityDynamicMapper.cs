using System.Data;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal interface IGenericDataReaderToEntityDynamicMapper : IDataReaderToEntityMapper //IDataReaderToEntityMapper<object>
    {
        int FieldsCount { get; }
    }

    internal class GenericDataReaderToEntityDynamicMapper : IGenericDataReaderToEntityDynamicMapper
    {
        private DynamicMapperHelper.Load Handler;
        public int FieldsCount { get; }

        public GenericDataReaderToEntityDynamicMapper(DynamicMapperHelper.Load handler, int fieldsCount)
        {
            Handler = handler;
            FieldsCount = fieldsCount;
        }

        public ValueTask<object> BuildAsync(IDataRecord dataRecord, int i)
        {
            return new ValueTask<object>(Handler(dataRecord));
        }
    }
}
