using Hector.Excel;
using OfficeOpenXml.Style;
using System.Drawing;

namespace Hector.Tests.Excel
{
    public class ExcelTests
    {
        [Fact]
        public async Task TestExcelFileCreationWithRemiraStyle()
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

            const string filePath = @"C:\temp\hector_excel.xlsx";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using FileStream stream = new(filePath, FileMode.CreateNew);
            await excelCreator.CreateExcelFileAsync("Report", data, stream, true, nameof(ExcelDTO.Code2).AsArray());
        }
    }

#nullable disable
    public record ExcelDTO(DateTime Date, int Id, string Code, string Code2);
}
