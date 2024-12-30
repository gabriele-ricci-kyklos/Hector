using Hector.Data.Entities;
using Hector.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Hector.Data.DataReaders
{
    public class EntityDbDataReader<T> : EnumerableDbDataReader<T>
        where T : IBaseEntity
    {
        private readonly EntityPropertyInfo[] _propertyInfoList;
        private Func<EntityPropertyInfo, (int? Precision, int? Scale)> RetrieveNumberPrecision { get; }
        private readonly Dictionary<string, int> _ordinalDict = [];

        public EntityDbDataReader(IEnumerable<T> values, Func<EntityPropertyInfo, (int? Precision, int? Scale)> retrieveNumberPrecision)
            : base(values)
        {
            _propertyInfoList = GetEntityPropertyInfoList(_type);

            RetrieveNumberPrecision = retrieveNumberPrecision;

            _ordinalDict =
                _propertyInfoList
                    .Select((x, i) => (Index: i, Name: x.ColumnName))
                    .Union
                    (
                        _propertyInfoList
                            .Select((x, i) => (Index: i, Name: x.PropertyName))
                    )
                    .ToDictionary(x => x.Name, x => x.Index);
        }

        private static EntityPropertyInfo[] GetEntityPropertyInfoList(Type type) =>
            EntityHelper
                .GetEntityPropertyInfoList(type)
                .OrderBy(x => x.ColumnOrder)
                .ToArray();

        protected override Dictionary<string, PropertyInfo> GetMembers() =>
            GetEntityPropertyInfoList(_type)
                ?.ToDictionary(x => x.PropertyName, x => x.PropertyInfo) ?? [];

        public override int GetOrdinal(string name)
        {
            if (_ordinalDict.TryGetValue(name, out int i))
            {
                return i;
            }

            throw new ArgumentOutOfRangeException($"No field or property named '{name}' found");
        }

        public override DataTable GetSchemaTable()
        {
            SchemaDataTable dt = new();
            dt.BeginLoadData();

            for (int i = 0; i < FieldCount; ++i)
            {
                DataRow row = dt.NewRow();
                EntityPropertyInfo fieldAttribute = _propertyInfoList[i];

                row[dt.ColumnNameColumn] = fieldAttribute.PropertyName;
                row[dt.ColumnOrdinalColumn] = i;

                bool isNullable =
                    fieldAttribute
                    ?.IsNullable
                    ?? (
                        fieldAttribute!
                            .Type
                            .IsNullableType()
                        ||
                        fieldAttribute
                            .Type
                            .IsClass
                    );

                row[dt.AllowDBNullColumn] = isNullable;
                row[dt.IsKeyColumn] = fieldAttribute?.IsPrimaryKey ?? false;
                row[dt.DataTypeColumn] = GetFieldType(i);
                row[dt.ColumnSizeColumn] = fieldAttribute?.MaxLength ?? -1;

                (int? precision, int? scale) =
                    fieldAttribute is null ?
                    (null, null)
                    : RetrieveNumberPrecision(fieldAttribute);

                row[dt.NumericPrecisionColumn] = !precision.HasValue ? DBNull.Value : precision;
                row[dt.NumericScaleColumn] = !scale.HasValue ? DBNull.Value : scale;

                dt.Rows.Add(row);
            }

            dt.AcceptChanges();
            dt.EndLoadData();

            return dt;
        }
    }
}

class SchemaDataTable : DataTable
{
    internal const string ColumnNameFieldName = "ColumnName";
    internal const string ColumnOrdinalFieldName = "ColumnOrdinal";
    internal const string AllowDBNullFieldName = "AllowDBNull";
    internal const string IsKeyFieldName = "IsKey";
    internal const string DataTypeFieldName = "DataType";
    internal const string ColumnSizeFieldName = "ColumnSize";
    internal const string NumericPrecisionFieldName = "NumericPrecision";
    internal const string NumericScaleFieldName = "NumericScale";

    internal DataColumn ColumnNameColumn { get; }
    internal DataColumn ColumnOrdinalColumn { get; }
    internal DataColumn AllowDBNullColumn { get; }
    internal DataColumn IsKeyColumn { get; }
    internal DataColumn DataTypeColumn { get; }
    internal DataColumn ColumnSizeColumn { get; }
    internal DataColumn NumericPrecisionColumn { get; }
    internal DataColumn NumericScaleColumn { get; }

    internal SchemaDataTable()
    {
        ColumnNameColumn = new DataColumn { ColumnName = ColumnNameFieldName, DataType = typeof(string), MaxLength = -1, AllowDBNull = false };
        ColumnOrdinalColumn = new DataColumn { ColumnName = ColumnOrdinalFieldName, DataType = typeof(int), AllowDBNull = true };
        AllowDBNullColumn = new DataColumn { ColumnName = AllowDBNullFieldName, DataType = typeof(bool), AllowDBNull = true };
        IsKeyColumn = new DataColumn { ColumnName = IsKeyFieldName, DataType = typeof(bool), AllowDBNull = true };
        DataTypeColumn = new DataColumn { ColumnName = DataTypeFieldName, DataType = typeof(Type), MaxLength = -1, AllowDBNull = false };
        ColumnSizeColumn = new DataColumn { ColumnName = ColumnSizeFieldName, DataType = typeof(int), MaxLength = -1, AllowDBNull = false };
        NumericPrecisionColumn = new DataColumn { ColumnName = NumericPrecisionFieldName, DataType = typeof(int), MaxLength = -1, AllowDBNull = true };
        NumericScaleColumn = new DataColumn { ColumnName = NumericScaleFieldName, DataType = typeof(int), MaxLength = -1, AllowDBNull = true };

        Columns.Add(ColumnNameColumn);
        Columns.Add(ColumnOrdinalColumn);
        Columns.Add(AllowDBNullColumn);
        Columns.Add(IsKeyColumn);
        Columns.Add(DataTypeColumn);
        Columns.Add(ColumnSizeColumn);
        Columns.Add(NumericPrecisionColumn);
        Columns.Add(NumericScaleColumn);
    }
}
