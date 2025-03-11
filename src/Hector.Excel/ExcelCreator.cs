using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hector.Excel
{
    public record ExcelCell(string ColumnName, object? Value);
    public record ExcelCreatorOptions(string Author, string Title, bool FormatIntegerValues = false, bool CreateHeaderRow = true, bool AutoFilterHeader = true,
        Action<ExcelStyle>? HeaderStyleFx = null, Action<int, ExcelStyle>? RowStyleFx = null, Dictionary<Type, string>? TypeFormatMap = null);


    public class ExcelCreator
    {
        private readonly ExcelCreatorOptions _options;

        private readonly Dictionary<Type, string> _typeFormatMap =
            new()
            {
                { typeof(decimal), "#,##0.00" },
                { typeof(DateTime), "yyyy-MM-dd" },
                { typeof(DateTime?), "yyyy-MM-dd" },
            };

        public ExcelCreator(ExcelCreatorOptions options)
        {
            _options = options;
            _typeFormatMap = _typeFormatMap.MergeRight(options.TypeFormatMap ?? []);

            if (options.FormatIntegerValues)
            {
                _typeFormatMap[typeof(int)] = "###,###,###,###";
                _typeFormatMap[typeof(short)] = "###,###,###,###";
            }
        }

        public async Task CreateExcelFileAsync<T>(string sheetName, T[] data, Stream stream, bool createHeaderRow = true, string[]? propertiesToExclude = null)
        {
            ExcelSheet sheet = ExcelSheet.FromObjects(sheetName, data, createHeaderRow | _options.CreateHeaderRow, propertiesToExclude);
            HashSet<string> propertiesToExcludeSet = new HashSet<string>(propertiesToExclude ?? []);
            await InternalCreateExcelFileAsync([sheet], stream, propertiesToExcludeSet).ConfigureAwait(false);
        }

        public async Task CreateExcelFileAsync(string sheetName, DataTable data, Stream stream, bool createHeaderRow = true, string[]? propertiesToExclude = null)
        {
            ExcelSheet sheet = ExcelSheet.FromDataTable(sheetName, data, createHeaderRow | _options.CreateHeaderRow, propertiesToExclude);
            HashSet<string> propertiesToExcludeSet = new HashSet<string>(propertiesToExclude ?? []);
            await InternalCreateExcelFileAsync([sheet], stream, propertiesToExcludeSet).ConfigureAwait(false);
        }

        private async Task InternalCreateExcelFileAsync(ExcelSheet[] data, Stream stream, HashSet<string> propertiesToExcludeSet)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using ExcelPackage excelPackage = new(stream);

            //Set some properties of the Excel document
            excelPackage.Workbook.Properties.Author = _options.Author;
            excelPackage.Workbook.Properties.Title = _options.Title;
            excelPackage.Workbook.Properties.Created = DateTime.Now;

            Type nullType = typeof(ExcelCell);

            List<ExcelWorksheet> worksheetList = [];

            try
            {

                foreach (ExcelSheet sheet in data)
                {
                    ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add(sheet.Name);
                    worksheetList.Add(worksheet);

                    int excelColumn = 0;
                    int excelRow = 1;

                    foreach (ExcelCell[] rows in sheet.Data)
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

                            if (_typeFormatMap.TryGetValue(cell.Value?.GetType() ?? nullType, out string format))
                            {
                                excelCell.Style.Numberformat.Format = format;
                            }
                        }
                    }

                    excelColumn = 0;

                    if (sheet.CreateHeaderRow)
                    {
                        foreach (string property in sheet.Data.First().Select(x => x.ColumnName))
                        {
                            if (propertiesToExcludeSet.Contains(property))
                            {
                                continue;
                            }

                            ExcelRange excelCell = worksheet.Cells[1, ++excelColumn];
                            excelCell.Value = property;
                        }
                    }

                    AutoFitColumns(worksheet);

                    worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column].AutoFilter = _options.AutoFilterHeader;

                    ColorRows(worksheet);
                }

                await excelPackage.SaveAsync().ConfigureAwait(false);
            }
            finally
            {
                worksheetList.ForEach(x => x.Dispose());
            }
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
            if (_options.HeaderStyleFx is not null)
            {
                ExcelRange headerRange = sheet.Cells[1, 1, 1, sheet.Dimension.End.Column];
                _options.HeaderStyleFx(headerRange.Style);
            }

            if (_options.RowStyleFx is not null)
            {
                for (int row = sheet.Dimension.Start.Row; row <= sheet.Dimension.End.Row; ++row)
                {
                    ExcelRange rowRange = sheet.Cells[row, 1, row, sheet.Dimension.End.Column];
                    _options.RowStyleFx(row, rowRange.Style);
                }
            }
        }
    }
}