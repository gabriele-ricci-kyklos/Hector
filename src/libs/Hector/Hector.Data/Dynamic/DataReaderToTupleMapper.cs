using Hector.Core;
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal abstract class BaseDataReaderToTupleMapper
    {
        protected ConstructorInfo _ctor = null!;
        protected Type[] _tupleTypes = null!;

        private readonly object _lockObject = new object();

        protected void BuildCtor(IDataRecord dataRecord, Type type)
        {
            if (_ctor is null)
            {
                lock (_lockObject)
                {
                    if (_ctor is not null)
                    {
                        return;
                    }

                    int fieldCount = dataRecord.FieldCount;
                    if (fieldCount > 8)
                    {
                        throw new Exception($"Tuple ariety cannot be greater than 8");
                    }

                    _ctor =
                        type
                        .GetConstructors()
                        .First();

                    _tupleTypes =
                        _ctor
                        .GetParameters()
                        .Select(x => x.ParameterType)
                        .ToArray();
                }
            }
        }
    }

    internal class DataReaderToTupleMapper : BaseDataReaderToTupleMapper, IDataReaderToEntityMapper
    {
        public Type Type { get; }

        public DataReaderToTupleMapper(Type type)
        {
            Type = type;
        }

        public ValueTask<object> BuildAsync(IDataRecord dataRecord, int i)
        {
            if (i == 1)
            {
                BuildCtor(dataRecord, Type);
            }

            var values =
                Enumerable
                .Range(0, dataRecord.FieldCount)
                .Select
                (
                    idx =>
                        dataRecord
                        .GetValue(idx)
                    .ConvertTo(_tupleTypes[idx])
                )
                .ToArray();

            var tuple = _ctor.Invoke(values);

            return new ValueTask<object>(tuple);
        }
    }
}
