using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal class DataReaderToDictionaryMapper : IDataReaderToEntityMapper
    {
        public ValueTask<object> BuildAsync(IDataRecord dataRecord, int i)
        {
            object dict =
                Enumerable
                .Range(0, dataRecord.FieldCount)
                .Select
                (
                    idx => new KeyValuePair<string, object>(dataRecord.GetName(idx), dataRecord.GetValue(idx))
                )
                .ToDictionary(x => x.Key, x => x.Value);

            return new ValueTask<object>(dict);
        }
    }
}
