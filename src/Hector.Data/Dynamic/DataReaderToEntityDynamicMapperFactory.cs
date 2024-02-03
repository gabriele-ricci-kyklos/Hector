using Hector.Core;
using Hector.Core.Reflection;
using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal static class DataReaderToEntityDynamicMapperFactory
    {
        //private static readonly KCache<string, object> cache = new(-1, false);

        internal static ValueTask<IGenericDataReaderToEntityDynamicMapper> CreateMapperAsync
        (
            Type type,
            IDataRecord dataRecord,
            bool ignoreCase,
            string key,
            Func<string, bool>? isStringDataTypeFx = null,
            Func<PropertyInfo, string>? property2FieldNameMapping = null
        )
        {
            if (key.IsNullOrBlankString())
            {
                throw new ArgumentException($"Key cannot be null or empty in {nameof(CreateMapperAsync)}.");
            }

            //TODO: use cache
            //Exception? error = null;
            //Type destinationType = type;

            //Func<string, object> factory =
            //    _ =>
            //    {
            //        try
            //        {
            //            return
            //                DynamicMapperHelper
            //                .CreateMapperImpl
            //                (
            //                    type,
            //                    destinationType,
            //                    dataRecord,
            //                    ignoreCase,
            //                    isStringDataTypeFx,
            //                    property2FieldNameMapping
            //                );
            //        }
            //        catch (Exception ex)
            //        {
            //            error = ex;
            //            throw;
            //        }
            //    };

            //var cacheValue = await
            //    cache
            //    .GetOrAddWithFactoryTask(key, factory)
            //    .ConfigureAwait(false);

            //if (error is not null)
            //{
            //    throw error;
            //}

            //return (GenericDataReaderToEntityDynamicMapper)cacheValue.Result.Value;

            var result =
                DynamicMapperHelper
                    .CreateMapperImpl
                    (
                        type,
                        type,
                        dataRecord,
                        ignoreCase,
                        isStringDataTypeFx,
                        property2FieldNameMapping
                    );

            return new ValueTask<IGenericDataReaderToEntityDynamicMapper>(result);
        }

        public static async ValueTask<DataReaderToDynamicTupleMapper> CreateMapperForTupleAsync
        (
            Type type,
            IDataRecord dataRecord,
            bool ignoreCase,
            string key,
            Func<string, bool>? isStringDataTypeFx = null,
            Func<PropertyInfo, string>? property2FieldNameMapping = null
        )
        {
            if (!type.IsTypeTuple() && !type.IsTypeValueTuple())
            {
                throw new ArgumentException($"Type is not a generic tuple: {type.FullName}");
            }

            var typeArguments = type.GenericTypeArguments;

            DataReaderToDynamicTupleMapper mapper = new DataReaderToDynamicTupleMapper(type);

            await
                mapper
                .CreateMappers
                (
                    typeArguments,
                    dataRecord,
                    ignoreCase,
                    key,
                    isStringDataTypeFx,
                    property2FieldNameMapping
                )
                .ConfigureAwait(false);

            return mapper;
        }
    }
}
