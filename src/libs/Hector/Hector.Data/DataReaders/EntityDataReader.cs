﻿using Hector.Core.Reflection;
using Hector.Data.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Hector.Data.DataReaders
{
    public class EntityDataReader<T> : EnumerableDataReader<T>
    {
        private readonly EntityPropertyInfo[] _propertyInfoList;
        private Func<EntityPropertyInfo, (int? Precision, int? Scale)> RetrieveNumberPrecision { get; }
        private readonly Dictionary<string, int> _ordinalDict = [];

        public EntityDataReader(IEnumerable<T> values, Func<EntityPropertyInfo, (int? Precision, int? Scale)> retrieveNumberPrecision)
            : base(values)
        {
            _propertyInfoList =
                EntityHelper
                    .GetEntityPropertyInfoList<T>()
                    .OrderBy(x => x.ColumnOrder)
                    .ToArray();

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

        protected override Dictionary<string, PropertyInfo> GetMembers() =>
            _propertyInfoList
                .ToDictionary(x => x.PropertyName, x => x.PropertyInfo);

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

                row.SetField(dt.ColumnNameColumn, fieldAttribute.PropertyName);
                row.SetField(dt.ColumnOrdinalColumn, i);

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

                row.SetField(dt.AllowDBNullColumn, isNullable);
                row.SetField(dt.IsKeyColumn, fieldAttribute?.IsPrimaryKey ?? false);
                row.SetField(dt.DataTypeColumn, GetFieldType(i));
                row.SetField(dt.ColumnSizeColumn, fieldAttribute?.MaxLength ?? -1);

                var (precision, scale) =
                    fieldAttribute is null ?
                    (null, null)
                    : RetrieveNumberPrecision(fieldAttribute);

                row.SetField(dt.NumericPrecisionColumn, precision);
                row.SetField(dt.NumericScaleColumn, scale);

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
