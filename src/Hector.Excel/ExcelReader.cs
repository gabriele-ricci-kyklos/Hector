using FastMember;
using Hector.Reflection;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Hector.Excel
{
    public static class ExcelReader
    {
        static ExcelReader()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public static T[] GetExcelWorksheet<T>(string filePath, bool hasHeader = true)
        {
            using ExcelPackage excelPackage = new(filePath);
            if (excelPackage.Workbook.Worksheets.Count < 1)
            {
                throw new NotSupportedException("No excel sheets have been found");
            }

            using ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

            return EnumerateExcelWorksheet<T>(worksheet, hasHeader).ToArray();
        }

        public static T[] GetExcelWorksheet<T>(Stream fileStream, bool hasHeader = true)
        {
            using ExcelPackage excelPackage = new(fileStream);
            if (excelPackage.Workbook.Worksheets.Count < 1)
            {
                throw new NotSupportedException("No excel sheets have been found");
            }

            using ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];

            return EnumerateExcelWorksheet<T>(worksheet, hasHeader).ToArray();
        }

        public static T[] GetExcelWorksheet<T>(ExcelWorksheet worksheet, bool hasHeader = true) =>
            EnumerateExcelWorksheet<T>(worksheet, hasHeader).ToArray();

        public static IEnumerable<T> EnumerateExcelWorksheet<T>(string filePath, bool hasHeader = true)
        {
            using ExcelPackage excelPackage = new(filePath);
            if (excelPackage.Workbook.Worksheets.Count < 1)
            {
                throw new NotSupportedException("No excel sheets have been found");
            }

            using ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
            return EnumerateExcelWorksheet<T>(worksheet, hasHeader);
        }

        public static IEnumerable<T> EnumerateExcelWorksheet<T>(Stream fileStream, bool hasHeader = true)
        {
            using ExcelPackage excelPackage = new(fileStream);
            if (excelPackage.Workbook.Worksheets.Count < 1)
            {
                throw new NotSupportedException("No excel sheets have been found");
            }

            using ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
            return EnumerateExcelWorksheet<T>(worksheet, hasHeader);
        }

        public static IEnumerable<T> EnumerateExcelWorksheet<T>(ExcelWorksheet worksheet, bool hasHeader = true)
        {
            Type type = typeof(T);
            TypeAccessor typeAccessor = TypeAccessor.Create(type);
            ObjectConstructor typeConstructorDelegate = ObjectActivator.CreateILConstructorDelegate(type);
            PropertyInfo[] properties = type.GetHierarchicalOrderedPropertyList();
            bool hasAttributes = type.HasPropertyAttribute<ExcelFieldAttribute>();

            Dictionary<string, PropertyInfo> propertiesDict = [];
            Dictionary<int, string> columnsDict = [];

            if (hasAttributes)
            {
                Dictionary<string, short> orderByFileHeaderDict = [];

                (PropertyInfo Prop, ExcelFieldAttribute Attrib)[] attributesData =
                    properties
                        .Select(x => (Prop: x, Attrib: x.GetCustomAttribute<ExcelFieldAttribute>()))
                        .Where(x => x.Attrib is not null)
                        .OrderBy(x => x.Attrib.Order)
                        .ToArray();

                if (hasHeader && attributesData.Any(x => x.Attrib.Order < 0))
                {
                    orderByFileHeaderDict =
                        ReadHeader(worksheet)
                        .ToDictionary(x => x.Value, x => (short)x.Key);
                }

                for (int i = 0; i < attributesData.Length; ++i)
                {
                    (PropertyInfo propertyInfo, ExcelFieldAttribute attribute) = attributesData[i];

                    short order = attribute.Order < 0 ? (short)(i + 1) : attribute.Order;
                    string columnName = attribute.ColumnName.ToNullIfBlank() ?? propertyInfo.Name;

                    if (!hasHeader && attribute.Order < 0)
                    {
                        throw new NotSupportedException("Unable to map the excel file, no order is given to the properties and no header is specified in the excel file");
                    }
                    else if (attribute.Order < 0)
                    {
                        order =
                            orderByFileHeaderDict
                                .GetValueOrDefault(columnName)
                                .GetNonNullOrThrow(nameof(order));
                    }

                    columnsDict.Add(order, columnName);
                    propertiesDict.Add(columnName, propertyInfo);
                }
            }
            else if (hasHeader)
            {
                columnsDict = ReadHeader(worksheet);
                propertiesDict = properties.ToDictionary(x => x.Name);
            }
            else
            {
                propertiesDict = properties.ToDictionary(x => x.Name);

                columnsDict =
                    propertiesDict
                        .Keys
                        .Select((x, i) => (Key: i + 1, Name: x))
                        .ToDictionary(x => x.Key, x => x.Name);
            }

            int fieldCount = columnsDict.Count;
            int row = hasHeader ? 1 : 0;

            do
            {
                T item = (T)typeConstructorDelegate();
                int column = 0; row++;

                for (int i = 1; i <= fieldCount; ++i)
                {
                    string columnName =
                        columnsDict.GetValueOrDefault(++column)
                        ?? throw new NotSupportedException($"No columns found by index {column}");

                    PropertyInfo property =
                        propertiesDict.GetValueOrDefault(columnName)
                        ?? throw new NotSupportedException($"No properties found by name {columnName}");

                    object? propValue = TryCastCellValue(worksheet, row, column, property.PropertyType);
                    typeAccessor[item, property.Name] = propValue;
                }

                yield return item;
            }
            while (HasNextExcelRowData(worksheet, row, fieldCount));
        }

        private static Dictionary<int, string> ReadHeader(ExcelWorksheet worksheet)
        {
            int column = 0, row = 1;
            Dictionary<int, string> columns = [];

            do
            {
                object cellValue =
                    worksheet
                        .Cells[row, ++column]
                        .Value;

                string propValue = cellValue.ToString();
                columns.Add(column, propValue);
            }
            while (worksheet.Cells[1, column + 1].Value is not null);

            return columns;
        }

        private static bool HasNextExcelRowData(ExcelWorksheet worksheet, int currentRow, int fieldCount)
        {
            int row = currentRow + 1;
            bool hasData = false;

            for (int i = 1; i <= fieldCount; ++i)
            {
                object cellValue = worksheet.Cells[row, i].Value;
                if ((cellValue is not null))
                {
                    hasData = true;
                    break;
                }
            }

            return hasData;
        }

        static readonly Type[] _dateTimeTypes = [typeof(DateTime), typeof(DateTime?)];

        private static object? TryCastCellValue(ExcelWorksheet worksheet, int row, int column, Type type)
        {
            try
            {
                object cellValue =
                    worksheet
                        .Cells[row, column]
                        .Value;

                if (_dateTimeTypes.Contains(type))
                {
                    string? strCellValue = cellValue?.ToString();
                    double? oADateValue = strCellValue.ToNumber<double>();
                    if (oADateValue.HasValue)
                    {
                        return DateTime.FromOADate(oADateValue.Value);
                    }
                }

                return cellValue?.ConvertTo(type) ?? type.GetDefaultValue();
            }
            catch (InvalidCastException)
            {
                return type.GetDefaultValue();
            }
        }
    }
}
