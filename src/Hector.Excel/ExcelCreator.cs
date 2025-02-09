using FastMember;
using Hector.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hector.Excel
{
    record ExcelCell(string ColumnName, object? Value);

    public class ExcelCreator
    {
        private readonly Dictionary<Type, string> _excelFormattingDict =
            new()
            {
                { typeof(decimal), "#,##0.00" },
                { typeof(DateTime), "dd/MM/yyyy" },
                { typeof(DateTime?), "dd/MM/yyyy" },
            };

        private readonly Color _headerColor = ColorTranslator.FromHtml("#4F81BD");
        private readonly Color _rowColor = ColorTranslator.FromHtml("#DCE6F1");
        private readonly bool _colorHeader = true;
        private readonly bool _colorRow = true;

        public ExcelCreator(Color? headerColor = null, Color? rowColor = null, bool colorHeader = true, bool colorRow = true, bool formatIntegerValues = false, Dictionary<Type, string>? excelFormattingRulesDict = null)
        {
            _headerColor = headerColor ?? _headerColor;
            _rowColor = rowColor ?? _rowColor;
            _colorHeader = colorHeader;
            _colorRow = colorRow;
            _excelFormattingDict = _excelFormattingDict.MergeRight(excelFormattingRulesDict ?? []);
            if (formatIntegerValues)
            {
                _excelFormattingDict[typeof(int)] = "###,###,###,###";
                _excelFormattingDict[typeof(short)] = "###,###,###,###";
            }
        }

        public async Task CreateExcelFileAsync<T>(string excelFileTitle, T[] data, Stream stream, string[]? propertiesToExclude = null)
        {
            Type type = typeof(T);
            TypeAccessor typeAccessor = TypeAccessor.Create(type);
            PropertyInfo[] properties = typeAccessor.GetHierarchicalOrderedPropertyList(type);
            HashSet<string> propertiesToExcludeSet = new(propertiesToExclude.ToEmptyIfNull(), StringComparer.OrdinalIgnoreCase);

            string[] propertyList =
                properties
                    .Select(x => x.Name)
                    .Where(x => !propertiesToExcludeSet.Contains(x))
                    .ToArray();

            List<ExcelCell[]> dataList = [];

            for (int i = 0; i < data.Length; ++i)
            {
                T item = data[i];
                if (item is null)
                {
                    continue;
                }

                Dictionary<string, object?> currentItemPropertyValues = item.GetPropertyValues(properties, propertiesToExclude);

                ExcelCell[] rowCellList =
                    currentItemPropertyValues
                        .Select(x => new ExcelCell(x.Key, x.Value))
                        .ToArray();

                dataList.Add(rowCellList);
            }

            await InternalCreateExcelFileAsync(excelFileTitle, dataList.ToArray(), stream, propertiesToExcludeSet).ConfigureAwait(false);
        }

        public async Task CreateExcelFileAsync(string excelFileTitle, DataTable data, Stream stream, string[]? propertiesToExclude = null)
        {
            HashSet<string> propertiesToExcludeSet = new(propertiesToExclude.ToEmptyIfNull(), StringComparer.OrdinalIgnoreCase);

            string[] columnList =
                data
                    .Columns
                    .Cast<DataColumn>()
                    .Select(x => x.ColumnName)
                    .Where(x => !propertiesToExcludeSet.Contains(x))
                    .ToArray();

            List<ExcelCell[]> dataList = [];

            foreach (DataRow row in data.Rows)
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

                dataList.Add(rowCellList.ToArray());
            }

            //TODO: check
            //if (datalist.count() == 0)
            //{
            //    throw new exceldatanotfoundexception();
            //}

            await InternalCreateExcelFileAsync(excelFileTitle, dataList.ToArray(), stream, propertiesToExcludeSet).ConfigureAwait(false);
        }

        public async Task CreateExcelFileAsync<T>(string excelFileTitle, T[] data, string filePath, string[]? propertiesToExclude = null)
        {
            using FileStream stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            await CreateExcelFileAsync(excelFileTitle, data, stream, propertiesToExclude);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        public async Task CreateExcelFileAsync(string excelFileTitle, DataTable data, string filePath, string[]? propertiesToExclude = null)
        {
            using FileStream stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
            await CreateExcelFileAsync(excelFileTitle, data, stream, propertiesToExclude);
        }

        private async Task InternalCreateExcelFileAsync(string excelFileTitle, ExcelCell[][] data, Stream stream, HashSet<string> propertiesToExcludeSet)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using ExcelPackage excelPackage = new(stream);

            //Set some properties of the Excel document
            excelPackage.Workbook.Properties.Author = "REMIRA Italia SRL";
            excelPackage.Workbook.Properties.Title = excelFileTitle;
            excelPackage.Workbook.Properties.Created = DateTime.Now;

            using ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Sheet 1");

            int excelColumn = 0;
            int excelRow = 1;
            excelColumn = 0;

            Type nullType = typeof(ExcelCell);

            foreach (ExcelCell[] rows in data)
            {
                excelRow += 1;
                excelColumn = 0;

                foreach (ExcelCell cell in rows)
                {
                    if (propertiesToExcludeSet.Contains(cell.ColumnName))
                    {
                        continue;
                    }

                    ExcelRange excelCell = worksheet.Cells[excelRow, ++excelColumn];
                    excelCell.Value = cell.Value;

                    if (_excelFormattingDict.TryGetValue(cell.Value?.GetType() ?? nullType, out string format))
                    {
                        excelCell.Style.Numberformat.Format = format;
                    }
                }
            }

            excelColumn = 0;

            //Creating header row
            foreach (string property in data.First().Select(x => x.ColumnName))
            {
                if (propertiesToExcludeSet.Contains(property))
                {
                    continue;
                }

                ExcelRange excelCell = worksheet.Cells[1, ++excelColumn];
                excelCell.Value = property;
            }

            AutoFitColumns(worksheet);

            worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column].AutoFilter = true;

            ColorRows(worksheet);

            await excelPackage.SaveAsync().ConfigureAwait(false);
        }

        private void AutoFitColumns(ExcelWorksheet sheet)
        {
            using Font stringFont = new(sheet.Cells.Style.Font.Name, sheet.Cells.Style.Font.Size, FontStyle.Regular);
            using Graphics g = Graphics.FromHwnd(IntPtr.Zero);

            g.PageUnit = GraphicsUnit.Pixel;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            for (int i = 1; i < sheet.Dimension.Columns + 1; ++i)
            {
                string longestString = string.Empty;

                foreach (ExcelRangeBase c in sheet.Cells[1, i, sheet.Dimension.End.Row, i])
                {
                    string cellText = c.Text.Trim();
                    longestString = cellText.Length > longestString.Length ? cellText : longestString;
                }

                //use the empty strings to give the cells a little padding so its not so cramped 
                SizeF stringSize = g.MeasureString(longestString, stringFont, int.MaxValue, StringFormat.GenericDefault);

                // the measured width is always 7.76 times greater than it should be
                sheet.Column(i).Width = (stringSize.Width / 5.8f);
            }
        }

        private void ColorRows(ExcelWorksheet sheet)
        {
            if (_colorHeader)
            {
                ExcelRange headerRange = sheet.Cells[1, 1, 1, sheet.Dimension.End.Column];
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(_headerColor);
                headerRange.Style.Font.Color.SetColor(Color.White);
                headerRange.Style.Font.Bold = true;
            }

            if (!_colorRow)
            {
                return;
            }

            for (int row = sheet.Dimension.Start.Row; row <= sheet.Dimension.End.Row; ++row)
            {
                if (row % 2 != 0)
                {
                    continue;
                }

                ExcelRange rowRange = sheet.Cells[row, 1, row, sheet.Dimension.End.Column];
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(_rowColor);
            }
        }
    }
}