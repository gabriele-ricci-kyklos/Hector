using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Hector.Core
{
    public static class DataExtensionMethods
    {
        // ToEntityList IEnumerable<DataRow>

        public static IEnumerable<T> ToEntityList<T>(this IEnumerable<DataRow> rows, bool throwIfPropertyNotFound = false)
            where T : new() =>
            ToEntityList<T>(rows, throwIfPropertyNotFound, StringComparison.OrdinalIgnoreCase);

        public static IEnumerable<T> ToEntityList<T>(this IEnumerable<DataRow> rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison)
            where T : new() =>
            ToEntityList<T>(rows, throwIfPropertyNotFound, propertyNameComparison, null, null);

        public static IEnumerable<T> ToEntityList<T>(this IEnumerable<DataRow> rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap)
            where T : new() =>
            ToEntityList<T>(rows, throwIfPropertyNotFound, propertyNameComparison, typesMap, null);

        public static IEnumerable<T> ToEntityList<T>(this IEnumerable<DataRow> rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<string, string>? propertyNamesMap)
            where T : new() =>
            ToEntityList<T>(rows, throwIfPropertyNotFound, propertyNameComparison, null, propertyNamesMap);

        public static IEnumerable<T> ToEntityList<T>(this IEnumerable<DataRow> rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap)
            where T : new()
        {
            foreach (DataRow dr in rows)
            {
                object newRow = ToEntity(dr, typeof(T), throwIfPropertyNotFound, propertyNameComparison, typesMap, propertyNamesMap);
                yield return (T)newRow;
            }
        }

        // ToEntityList DataTable

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound = false)
            where T : new() =>
            ToEntityList<T>(table, throwIfPropertyNotFound, StringComparison.InvariantCultureIgnoreCase);

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison)
            where T : new() =>
            ToEntityList<T>(table, throwIfPropertyNotFound, propertyNameComparison, null, null);

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap)
            where T : new() =>
            ToEntityList<T>(table, throwIfPropertyNotFound, propertyNameComparison, typesMap, null);

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<string, string>? propertyNamesMap)
            where T : new() =>
            ToEntityList<T>(table, throwIfPropertyNotFound, propertyNameComparison, null, propertyNamesMap);

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap)
            where T : new() =>
            table
                .Rows
                .Cast<DataRow>()
                .ToEntityList<T>(throwIfPropertyNotFound, propertyNameComparison, typesMap, propertyNamesMap);

        public static object ToEntity(this DataRow tableRow, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap)
        {
            object returnObj =
                Activator
                    .CreateInstance(type)
                    .GetNonNullOrThrow(nameof(returnObj));

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (DataColumn col in tableRow.Table.Columns)
            {
                PropertyInfo? propertyInfo = null;
                FieldInfo? fieldInfo = null;

                string? mappedPropertyName = null;

                if (propertyNamesMap?.ContainsKey(col.ColumnName) ?? false)
                {
                    mappedPropertyName = propertyNamesMap[col.ColumnName];
                }

                propertyInfo = properties.FirstOrDefault(x => x.Name.Equals(mappedPropertyName ?? col.ColumnName, propertyNameComparison));

                Func<object, Type, object> cellConverter =
                    typesMap is null
                    ? (obj, t) => obj.ConvertTo(t)
                    : (obj, t) => typesMap[t];

                if (propertyInfo != null)
                {
                    object value = cellConverter(tableRow[col], propertyInfo.PropertyType);
                    returnObj.SetPropertyValue(propertyInfo.Name, value);
                }
                else
                {
                    fieldInfo = fields.FirstOrDefault(x => x.Name.Equals(mappedPropertyName ?? col.ColumnName, propertyNameComparison));

                    if (fieldInfo != null)
                    {
                        object value = cellConverter(tableRow[col], fieldInfo.FieldType);
                        returnObj.SetFieldValue(fieldInfo.Name, value);
                    }
                    else if (throwIfPropertyNotFound)
                    {
                        throw new ArgumentException($"The property '{col.ColumnName}' has not been found using the comparison '{propertyNameComparison}'");
                    }
                }
            }

            return returnObj;
        }
    }
}
