using Hector.Data.Entities;
using Hector.Data.Entities.Attributes;

namespace Hector.Data.SqlServer
{
    public class SqlServerAsyncDaoHelper : BaseAsyncDaoHelper
    {
        private const int _decimalNumericPrecision = 18;
        private const int _decimalNumericScale = 5;

        public override string ParameterPrefix => "@";

        public override string StringConcatOperator => "+";

        public override string EscapeLeftStr => "[";

        public override string EscapeRightStr => "]";

        public override string SubstringFunction => "substring({0}, {1}, {2})";

        public override string TrimStartFunction => "ltrim({0})";

        public override string TrimEndFunction => "rtrim({0})";

        public override string TrimFunction => "ltrim(rtrim({0}))";

        public override string UpperFunction => "upper({0})";

        public override string LowerFunction => "lower({0})";

        public override string LengthFunction => "len({0})";

        public override string ReplaceFunction => "replace({0}, {1})";

        public override string IsNullFunction => "isnull({0}, {1})";

        public override string SequenceValue => "(next value for {0})";

        public override (int? Precision, int? Scale) GetNumericPrecision(EntityPropertyInfo entityPropertyInfo) =>
            entityPropertyInfo.DbType switch
            {
                PropertyDbType.Float => (7, 255),
                PropertyDbType.Double => (15, 255),
                PropertyDbType.Decimal => (_decimalNumericPrecision, _decimalNumericScale),
                PropertyDbType.Numeric => (entityPropertyInfo.NumericPrecision, entityPropertyInfo.NumericScale),
                _ => (null, null)
            };

        public override string MapDbTypeToSqlType(EntityPropertyInfo propertyInfo)
        {
            (int? precision, int? scale) = GetNumericPrecision(propertyInfo);

            return
                propertyInfo.DbType switch
                {
                    PropertyDbType.Blob or
                    PropertyDbType.ByteArray => "VARBINARY(MAX)",
                    PropertyDbType.Boolean => "BIT",
                    PropertyDbType.Byte => "TINYINT",
                    PropertyDbType.Clob => "NVARCHAR(MAX)",
                    PropertyDbType.DateTime => "DATETIME",
                    PropertyDbType.DateTime2 => "DATETIME2",
                    PropertyDbType.Decimal => $"DECIMAL(18, 5)",
                    PropertyDbType.Double => "FLOAT",
                    PropertyDbType.Float => "REAL",
                    PropertyDbType.Integer => "INT",
                    PropertyDbType.Long => "BIGINT",
                    PropertyDbType.Short => "SMALLINT",
                    PropertyDbType.String => $"NVARCHAR({propertyInfo.MaxLength})",
                    PropertyDbType.Numeric => $"NUMERIC({precision ?? 0}, {scale ?? 0})",
                    _ => string.Empty
                };
        }
    }
}
