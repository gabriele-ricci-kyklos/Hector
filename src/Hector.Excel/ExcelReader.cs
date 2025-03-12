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

            int fieldCount = properties.Length;
            int row = hasHeader ? 1 : 0;

            do
            {
                T item = (T)typeConstructorDelegate();
                int column = 0; row++;

                foreach (PropertyInfo prop in properties)
                {
                    object cellValue =
                        worksheet
                            .Cells[row, ++column]
                            .Value;

                    object? propValue = TryCastCellValue(cellValue, prop.PropertyType);
                    typeAccessor[item, prop.Name] = propValue;
                }

                yield return item;
            }
            while (HasNextExcelRowData(worksheet, row, fieldCount));
        }

        private static bool HasNextExcelRowData(ExcelWorksheet worksheet, int currentRow, int fieldCount)
        {
            int row = currentRow + 1;
            bool hasData = false;

            for (int i = 1; i <= fieldCount && !hasData; ++i)
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

        private static object? TryCastCellValue(object cellValue, Type type)
        {
            try
            {
                if(type == typeof(DateTime) && cellValue.TryConvertTo<double>(out double oADateValue))
                {
                    DateTime dateTime = DateTime.FromOADate(oADateValue);
                    return dateTime;
                }

                object? castedValue = cellValue?.ConvertTo(type) ?? type.GetDefaultValue();
                return castedValue;
            }
            catch (InvalidCastException)
            {
                return type.GetDefaultValue();
            }
        }
    }
}
