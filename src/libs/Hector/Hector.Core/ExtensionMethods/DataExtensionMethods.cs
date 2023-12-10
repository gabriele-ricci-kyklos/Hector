using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Hector.Core
{
    public static class DataExtensionMethods
    {
        // ToEntityList DataRow[]

        public static IEnumerable<T> ToEntityList<T>(this DataRow[] rows, bool throwIfPropertyNotFound = false) where T : new()
        {
            return ToEntityList<T>(rows, throwIfPropertyNotFound, StringComparison.InvariantCultureIgnoreCase, null, null);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataRow[] rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison) where T : new()
        {
            return ToEntityList<T>(rows, throwIfPropertyNotFound, propertyNameComparison, null, null);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataRow[] rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap) where T : new()
        {
            return ToEntityList<T>(rows, throwIfPropertyNotFound, propertyNameComparison, typesMap, null);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataRow[] rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<string, string>? propertyNamesMap) where T : new()
        {
            return ToEntityList<T>(rows, throwIfPropertyNotFound, propertyNameComparison, null, propertyNamesMap);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataRow[] rows, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap) where T : new()
        {
            return
                ToEntityList(rows, typeof(T), throwIfPropertyNotFound, propertyNameComparison, typesMap, propertyNamesMap)
                .Cast<T>();
        }

        public static IEnumerable<object> ToEntityList(this DataRow[] rows, Type type, bool throwIfPropertyNotFound = false)
        {
            return ToEntityList(rows, type, throwIfPropertyNotFound, StringComparison.InvariantCultureIgnoreCase, null, null);
        }

        public static IEnumerable<object> ToEntityList(this DataRow[] rows, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison)
        {
            return ToEntityList(rows, type, throwIfPropertyNotFound, propertyNameComparison, null, null);
        }

        public static IEnumerable<object> ToEntityList(this DataRow[] rows, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap)
        {
            return ToEntityList(rows, type, throwIfPropertyNotFound, propertyNameComparison, typesMap, null);
        }

        public static IEnumerable<object> ToEntityList(this DataRow[] rows, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<string, string>? propertyNamesMap)
        {
            return ToEntityList(rows, type, throwIfPropertyNotFound, propertyNameComparison, null, propertyNamesMap);
        }

        public static IEnumerable<object> ToEntityList(this DataRow[] rows, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap)
        {
            foreach (DataRow dr in rows)
            {
                object newRow = ToEntity(dr, type, throwIfPropertyNotFound, propertyNameComparison, typesMap, propertyNamesMap);
                yield return newRow;
            }
        }

        // ToEntityList DataTable

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound = false) where T : new()
        {
            return ToEntityList<T>(table, throwIfPropertyNotFound, StringComparison.InvariantCultureIgnoreCase, null, null);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison) where T : new()
        {
            return ToEntityList<T>(table, throwIfPropertyNotFound, propertyNameComparison, null, null);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap) where T : new()
        {
            return ToEntityList<T>(table, throwIfPropertyNotFound, propertyNameComparison, typesMap, null);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<string, string>? propertyNamesMap) where T : new()
        {
            return ToEntityList<T>(table, throwIfPropertyNotFound, propertyNameComparison, null, propertyNamesMap);
        }

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap) where T : new()
        {
            return
                ToEntityList(table, typeof(T), throwIfPropertyNotFound, propertyNameComparison, typesMap, propertyNamesMap)
                    .Cast<T>();
        }

        public static IEnumerable<object> ToEntityList(this DataTable table, Type type, bool throwIfPropertyNotFound = false)
        {
            return ToEntityList(table, type, throwIfPropertyNotFound, StringComparison.InvariantCultureIgnoreCase, null, null);
        }

        public static IEnumerable<object> ToEntityList(this DataTable table, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison)
        {
            return ToEntityList(table, type, throwIfPropertyNotFound, propertyNameComparison, null, null);
        }

        public static IEnumerable<object> ToEntityList(this DataTable table, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap)
        {
            return ToEntityList(table, type, throwIfPropertyNotFound, propertyNameComparison, typesMap, null);
        }

        public static IEnumerable<object> ToEntityList(this DataTable table, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<string, string>? propertyNamesMap)
        {
            return ToEntityList(table, type, throwIfPropertyNotFound, propertyNameComparison, null, propertyNamesMap);
        }

        public static IEnumerable<object> ToEntityList(this DataTable table, Type type, bool throwIfPropertyNotFound, StringComparison propertyNameComparison, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap)
        {
            foreach (DataRow row in table.Rows)
            {
                yield return ToEntity(row, type, throwIfPropertyNotFound, propertyNameComparison, typesMap, propertyNamesMap);
            }
        }

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
