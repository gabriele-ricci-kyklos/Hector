using System.Data;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal interface IDataReaderToEntityMapper
    {
        ValueTask<object> BuildAsync(IDataRecord dataRecord, int i);
    }
}
