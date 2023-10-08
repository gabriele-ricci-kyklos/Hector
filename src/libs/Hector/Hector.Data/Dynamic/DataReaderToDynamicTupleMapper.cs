using Hector.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal abstract class BaseDataReaderToDynamicTupleMapper
    {
        protected IDataReaderToEntityMapper[] _mappers = null!;

        protected readonly ConstructorInfo _ctor;

        internal BaseDataReaderToDynamicTupleMapper(Type type)
        {
            _ctor =
                type
                .GetConstructors()
                .First();
        }

        internal async Task CreateMappers
        (
            Type[] tupleTypes,
            IDataRecord dataRecord,
            bool ignoreCase,
            string key,
            Func<string, bool>? isStringDataTypeFx,
            Func<PropertyInfo, string>? property2FieldNameMapping
        )
        {
            _mappers = new IDataReaderToEntityMapper[tupleTypes.Length];

            int fieldPosition = 0;

            for (int i = 0; i < tupleTypes.Length; ++i)
            {
                var type = tupleTypes[i];

                if (type.IsSimpleType() || type == typeof(byte[]) || type == typeof(char[]))
                {
                    _mappers[i] = new DataReaderToSingleValueMapper(fieldPosition, type);
                    ++fieldPosition;
                }
                else if (type.TypeIsValueTuple() || type.TypeIsTuple() || type.TypeIsDictionary())
                {
                    throw new NotSupportedException($"{type.FullName} cannot be nested inside a tuple");
                }
                else
                {
                    var mapper =
                        await
                        DataReaderToEntityDynamicMapperFactory
                        .CreateMapperAsync(type, dataRecord, ignoreCase, $"{key}_{type.AssemblyQualifiedName}", isStringDataTypeFx, property2FieldNameMapping)
                        .ConfigureAwait(false);

                    fieldPosition += mapper.FieldsCount;

                    _mappers[i] = mapper;
                }
            }
        }
    }

    internal class DataReaderToDynamicTupleMapper : BaseDataReaderToDynamicTupleMapper, IDataReaderToEntityMapper
    {
        public DataReaderToDynamicTupleMapper(Type type) : base(type)
        {
        }

        public async ValueTask<object> BuildAsync(IDataRecord dataRecord, int i)
        {
            List<Task<object>> tasks = new();
            foreach (var m in _mappers)
            {
                tasks.Add(m.BuildAsync(dataRecord, i).AsTask());
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var values = tasks.Select(t => t.Result).ToArray();

            //var values = await
            //    _mappers
            //    .ToAsyncEnumerable()
            //    .SelectAwait(async m => await m.BuildAsync(dataRecord, i).ConfigureAwait(false))
            //    .ToArrayAsync()
            //    .ConfigureAwait(false);

            var tuple = _ctor.Invoke(values);
            return tuple;
        }
    }
}
