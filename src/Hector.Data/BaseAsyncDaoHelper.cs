using Hector.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data;

namespace Hector.Data
{
    public abstract class BaseAsyncDaoHelper : IAsyncDaoHelper
    {
        private static readonly Dictionary<Type, DbType> _typeToDbMapping;

        static BaseAsyncDaoHelper()
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

        public abstract string ParameterPrefix { get; }

        public abstract string StringConcatOperator { get; }

        public abstract string EscapeLeftStr { get; }

        public abstract string EscapeRightStr { get; }

        public abstract string SubstringFunction { get; }

        public abstract string TrimStartFunction { get; }

        public abstract string TrimEndFunction { get; }

        public abstract string TrimFunction { get; }

        public abstract string UpperFunction { get; }

        public abstract string LowerFunction { get; }

        public abstract string LengthFunction { get; }

        public abstract string ReplaceFunction { get; }

        public abstract string IsNullFunction { get; }

        public abstract string SequenceValue { get; }

        public virtual string DummyTableName => string.Empty;

        public string EscapeFieldName(string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            fieldName = fieldName.Trim();

            if (!fieldName.StartsWith(EscapeLeftStr))
            {
                fieldName = EscapeLeftStr + fieldName;
            }
            if (!fieldName.EndsWith(EscapeRightStr))
            {
                fieldName += EscapeRightStr;
            }

            return fieldName;
        }

        public static DbType MapTypeToDbType(Type type)
        {
            if (!_typeToDbMapping.TryGetValue(type, out DbType value))
            {
                throw new NotSupportedException($"The type {type.Name} is not mapped to any DbType");
            }

            return value;
        }

        public virtual string BuildParameterName(string name) => $"{ParameterPrefix}P{name}";
        public virtual string BuildParameterName(int i) => $"{ParameterPrefix}P{i}";

        public abstract (int? Precision, int? Scale) GetNumericPrecision(EntityPropertyInfo entityPropertyInfo);
    }
}
