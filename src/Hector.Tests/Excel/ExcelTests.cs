using FluentAssertions;
using Hector.Excel;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Hector.Tests.Excel
{
    public class ExcelTests
    {
        const string _filePath = @"C:\temp\hector_excel.xlsx";

        private async Task WriteExcelFile(bool withHeader)
        {
            ExcelDTO[] data = [
                new ExcelDTO(DateTime.Now, 1, "one", "1"),
                new ExcelDTO(DateTime.Now, 2, "two", "2"),
                new ExcelDTO(DateTime.Now, 3, "three", "3")
            ];

            ExcelCreatorOptions options =
                new
                (
                    "REMIRA Italia SRL",
                    "Excel report",
                    TypeFormatMap: new Dictionary<Type, string>
                    {
                        { typeof(DateTime), "dd/MM/yyyy" },
                        { typeof(DateTime?), "dd/MM/yyyy" }
                    },
                    HeaderStyleFx: (s) =>
                    {
                        s.Fill.PatternType = ExcelFillStyle.Solid;
                        s.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#4F81BD"));
                        s.Font.Color.SetColor(Color.White);
                        s.Font.Bold = true;
                    },
                    RowStyleFx: (i, s) =>
                    {
                        if (i % 2 != 0)
                        {
                            return;
                        }

                        s.Fill.PatternType = ExcelFillStyle.Solid;
                        s.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#DCE6F1"));
                    }
                );

            ExcelCreator excelCreator = new(options);

            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            using FileStream stream = File.OpenWrite(_filePath);
            await excelCreator.CreateExcelFileAsync("Report", data, stream, propertiesToExclude: nameof(ExcelDTO.Code2).AsArray());

            File.Exists(_filePath).Should().BeTrue();
        }

        [Fact]
        public Task TestExcelFileCreationWithRemiraStyle() =>
            WriteExcelFile(true);

        [Fact]
        public void TestExcelFileRead()
        {
            ExcelDTO[] items = ExcelReader.GetExcelWorksheet<ExcelDTO>(_filePath);
            items.Should().NotBeNullOrEmpty();
        }
    }

#nullable disable
    public class ExcelDTO(DateTime date, int id, string code, string code2)
    {
        [ExcelField(1)]
        public DateTime Date { get; set; } = date;
        [ExcelField(2)]
        public int Id { get; set; } = id;
        [ExcelField(3)]
        public string Code { get; set; } = code;
        public string Code2 { get; set; } = code2;

        public ExcelDTO() : this(default, default, null, null)
        {
        }
    }
}
