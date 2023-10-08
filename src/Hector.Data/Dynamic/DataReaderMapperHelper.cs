using Hector.Core;
using Hector.Core.Cryptography;
using Hector.Data.Entities.Attributes;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hector.Data.Dynamic
{
    internal static class DataReaderMapperHelper
    {
        internal static Func<DbDataReader, ValueTask<IDataReaderToEntityMapper>> CreateMapperFunction
        (
            Type type,
            Func<string?, bool> isStringDataType,
            int fieldPosition = 0,
            Func<Type, IDataReader, string>? typeHasher = null
        )
        {
            Func<DbDataReader, ValueTask<IDataReaderToEntityMapper>>? mapperCreator = null;

            if (type.IsSimpleType() || type == typeof(byte[]))
            {
                mapperCreator = _ => new ValueTask<IDataReaderToEntityMapper>(new DataReaderToSingleValueMapper(fieldPosition, type));
            }
            else if (TypeIsTupleOfSimpleTypes(type) || TypeIsValueTupleOfSimpleTypes(type))
            {
                mapperCreator = _ => new ValueTask<IDataReaderToEntityMapper>(new DataReaderToTupleMapper(type));
            }
            else if (type.TypeIsDictionary())
            {
                mapperCreator = _ => new ValueTask<IDataReaderToEntityMapper>(new DataReaderToDictionaryMapper());
            }
            else if (TypeIsTupleOfMixedTypes(type) || TypeIsValueTupleOfMixedTypes(type) || TypeIsTupleOrValueTupleOfComplexTypes(type))
            {
                mapperCreator =
                    async reader =>
                        await
                        DataReaderToEntityDynamicMapperFactory
                        .CreateMapperForTupleAsync
                        (
                            type: type,
                            dataRecord: reader,
                            ignoreCase: true,
                            key: (typeHasher ?? BuildHashForType)(type, reader),
                            isStringDataTypeFx: isStringDataType,
                            property2FieldNameMapping: pi => pi.GetFieldName()
                        )
                        .ConfigureAwait(false);
            }
            else
            {
                mapperCreator =
                    async reader =>
                        (await
                        DataReaderToEntityDynamicMapperFactory
                        .CreateMapperAsync
                        (
                            type: type,
                            dataRecord: reader,
                            ignoreCase: true,
                            key: (typeHasher ?? BuildHashForType)(type, reader),
                            isStringDataTypeFx: isStringDataType,
                            property2FieldNameMapping: pi => pi.GetFieldName()
                        )
                        .ConfigureAwait(false))!;
            }
            return mapperCreator;
        }

        private static bool TypeIsTupleOfSimpleTypes(this Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                typeArguments.All(x => x.IsSimpleType())
                && type.TypeIsTuple();
        }

        private static bool TypeIsValueTupleOfSimpleTypes(this Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                typeArguments.All(x => x.IsSimpleType())
                && type.TypeIsValueTuple();
        }

        private static bool TypeIsTupleOfMixedTypes(this Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                type.TypeIsTuple()
                && typeArguments.Any(x => x.IsSimpleType())
                && typeArguments.Any(x => !x.IsSimpleType());
        }

        private static bool TypeIsValueTupleOfMixedTypes(Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                type.TypeIsValueTuple()
                && typeArguments.Any(x => x.IsSimpleType())
                && typeArguments.Any(x => !x.IsSimpleType());
        }

        private static bool TypeIsTupleOrValueTupleOfComplexTypes(Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                (type.TypeIsTuple() || type.TypeIsValueTuple())
                && typeArguments.All(x => !x.IsSimpleType());
        }

        private static string BuildHashForType(Type? type, IDataReader? reader)
        {
            return HashHelper.ComputeSHA512((type?.FullName ?? "NULL TYPE") + "_" + BuildReaderDesc(reader));
        }

        private static string BuildReaderDesc(IDataReader? dataReader)
        {
            IDataReader dataReader2 = dataReader!;
            if (dataReader2 == null)
            {
                return "NULL READER";
            }

            return (from i in Enumerable.Range(0, dataReader2.FieldCount)
                    select dataReader2.GetName(i) + ":" + dataReader2.GetDataTypeName(i)).StringJoin("-");
        }

        private readonly static ConcurrentDictionary<PropertyInfo, EntityPropertyInfoAttribute?> entityPropertyInfoAttributeForProperties =
            new ConcurrentDictionary<PropertyInfo, EntityPropertyInfoAttribute?>();

        private static EntityPropertyInfoAttribute? GetEntityPropertyInfoAttributeFromCache(this PropertyInfo property) =>
            entityPropertyInfoAttributeForProperties
            .GetOrAdd
            (
                property,
                p => p.GetAttributeOfType<EntityPropertyInfoAttribute>(true)
            );

        public static string GetFieldName(this PropertyInfo property, bool usePropertyNameForNonDecoratedProperties = true)
        {
            if (property is null)
            {
                return string.Empty;
            }

            var attrib = property.GetEntityPropertyInfoAttributeFromCache();

            if (attrib is not null && !attrib.ColumnName.IsNullOrBlankString())
            {
                return attrib.ColumnName;
            }

            if (usePropertyNameForNonDecoratedProperties)
            {
                return property.Name;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
