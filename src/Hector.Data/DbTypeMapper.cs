using System;
using System.Collections.Generic;
using System.Data;

namespace Hector.Data
{
    internal static class DbTypeMapper
    {
        private readonly static Dictionary<Type, DbType> _typeToDbMapping;

        static DbTypeMapper()
        {
            _typeToDbMapping =
                new Dictionary<Type, DbType>
                {
                    { typeof(long), DbType.Int64 },
                    { typeof(ulong), DbType.UInt64 },
                    { typeof(int), DbType.Int32 },
                    { typeof(uint), DbType.UInt32 },
                    { typeof(short), DbType.Int16 },
                    { typeof(ushort), DbType.UInt16 },
                    { typeof(float), DbType.Single },
                    { typeof(double), DbType.Double },
                    { typeof(decimal), DbType.Decimal },
                    { typeof(byte[]), DbType.Binary },
                    { typeof(bool), DbType.Boolean },
                    { typeof(char), DbType.String },
                    { typeof(char[]), DbType.String },
                    { typeof(string), DbType.String },

                    { typeof(long?), DbType.Int64 },
                    { typeof(ulong?), DbType.UInt64 },
                    { typeof(int?), DbType.Int32 },
                    { typeof(uint?), DbType.UInt32 },
                    { typeof(short?), DbType.Int16 },
                    { typeof(ushort?), DbType.UInt16 },
                    { typeof(float?), DbType.Single },
                    { typeof(double?), DbType.Double },
                    { typeof(decimal?), DbType.Decimal },
                    { typeof(bool?), DbType.Boolean },
                    { typeof(char?), DbType.String },

                    { typeof(object), DbType.Object },
                    { typeof(DateTime), DbType.DateTime },
                    { typeof(DateTimeOffset), DbType.DateTimeOffset },
                    { typeof(TimeSpan), DbType.Time },
                    { typeof(Guid), DbType.Guid },
                };
        }

        internal static DbType MapTypeToDbType(Type type)
        {
            if (!_typeToDbMapping.TryGetValue(type, out DbType value))
            {
                throw new NotSupportedException($"The type {type.Name} is not mapped to any DbType");
            }

            return value;
        }
    }
}
