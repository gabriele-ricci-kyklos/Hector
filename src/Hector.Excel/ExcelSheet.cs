using Hector.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Hector.Excel
{
    internal class ExcelSheet
    {
        internal string Name { get; }
        internal bool CreateHeaderRow { get; set; }
        internal ExcelCell[][] Data { get; }

        private ExcelSheet(string name, bool createHeaderRow, ExcelCell[][] data)
        {
            Name = name;
            CreateHeaderRow = createHeaderRow;
            Data = data;
        }

        internal static ExcelSheet FromDataTable(string name, DataTable table, bool createHeaderRow = true, string[]? propertiesToExclude = null)
        {
            HashSet<string> propertiesToExcludeSet = new(propertiesToExclude.ToEmptyIfNull(), StringComparer.OrdinalIgnoreCase);

            string[] columnList =
                table
                    .Columns
                    .Cast<DataColumn>()
                    .Select(x => x.ColumnName)
                    .Where(x => !propertiesToExcludeSet.Contains(x))
                    .ToArray();

            List<ExcelCell[]> cellMatrix = [];

            foreach (DataRow row in table.Rows)
            {
                if (row.ItemArray.IsNullOrEmptyList())
                {
                    continue;
                }

                List<ExcelCell> rowCellList = [];

                foreach (string columnName in columnList)
                {
                    rowCellList.Add(new(columnName, row[columnName]));
                }

                cellMatrix.Add(rowCellList.ToArray());
            }

            return new ExcelSheet(name, createHeaderRow, cellMatrix.ToArray());
        }

        internal static ExcelSheet FromObjects<T>(string name, T[] data, bool createHeaderRow = true, string[]? propertiesToExclude = null)
        {
            PropertyInfo[] properties = typeof(T).GetHierarchicalOrderedPropertyList(propertiesToExclude);

            List<ExcelCell[]> cellMatrix = [];

            for (int i = 0; i < data.Length; ++i)
            {
                T item = data[i];
                if (item is null)
                {
                    continue;
                }

                Dictionary<string, object?> currentItemPropertyValues = item.GetPropertyValues(properties);

                ExcelCell[] rowCellList =
                    currentItemPropertyValues
                        .Select(x => new ExcelCell(x.Key, x.Value))
                        .ToArray();

                cellMatrix.Add(rowCellList);
            }

            return new ExcelSheet(name, createHeaderRow, cellMatrix.ToArray());
        }
    }
}
