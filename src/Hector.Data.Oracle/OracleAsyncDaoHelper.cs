using Hector.Data.Entities;
using Hector.Data.Entities.Attributes;

namespace Hector.Data.Oracle
{
    public class OracleAsyncDaoHelper : BaseAsyncDaoHelper
    {
        public override string ParameterPrefix => ":";

        public override string StringConcatOperator => "||";

        public override string EscapeLeftStr => "\"";

        public override string EscapeRightStr => EscapeLeftStr;

        public override string SubstringFunction => "substr({0}, {1}, {2})";

        public override string TrimStartFunction => "trim(leading ' ' from {0})";

        public override string TrimEndFunction => "trim(trailing ' ' from {0})";

        public override string TrimFunction => "trim(both ' ' from {0})";

        public override string UpperFunction => "upper({0})";

        public override string LowerFunction => "lower({0})";

        public override string LengthFunction => "length({0})";

        public override string ReplaceFunction => "replace({0}, {1})";

        public override string IsNullFunction => "nvl({0}, {1})";

        public override string DummyTableName => "dual";

        public override string SequenceValue => "({0}.nextval)";

        public override (int? Precision, int? Scale) GetNumericPrecision(EntityPropertyInfo entityPropertyInfo) =>
            entityPropertyInfo.DbType switch
            {
                PropertyDbType.Byte => (3, 0),
                PropertyDbType.Short => (5, 0),
                PropertyDbType.Integer => (10, 0),
                PropertyDbType.Long => (20, 0),
                PropertyDbType.Float => (15, 4),
                PropertyDbType.Double => (20, 6),
                PropertyDbType.Decimal => (30, 8),
                PropertyDbType.Numeric => (entityPropertyInfo.NumericPrecision, entityPropertyInfo.NumericScale),
                _ => (null, null)
            };

        public override string MapDbTypeToSqlType(EntityPropertyInfo propertyInfo)
        {
            (int? precision, int? scale) = GetNumericPrecision(propertyInfo);

            return 
                propertyInfo.DbType switch
                {
                    PropertyDbType.Blob => "BLOB",
                    PropertyDbType.Boolean => "NUMBER(1, 0)",
                    PropertyDbType.Byte => "NUMBER(3, 0)",
                    PropertyDbType.ByteArray => "BLOB",
                    PropertyDbType.Clob => "NCLOB",
                    PropertyDbType.DateTime => "DATE",
                    PropertyDbType.DateTime2 => "TIMESTAMP",
                    PropertyDbType.Decimal => "NUMBER(30, 8)",
                    PropertyDbType.Double => "NUMBER(20, 6)",
                    PropertyDbType.Float => "NUMBER(15, 4)",
                    PropertyDbType.Integer => "NUMBER(10, 0)",
                    PropertyDbType.Long => "NUMBER(20, 0)",
                    PropertyDbType.None => string.Empty,
                    PropertyDbType.Short => "NUMBER(5, 0)",
                    PropertyDbType.String => $"NVARCHAR2({propertyInfo.MaxLength})",
                    PropertyDbType.Numeric => $"NUMERIC({precision ?? 0}, {scale ?? 0})",
                    _ => string.Empty
                };
        }
    }
}
