using ExcelDataReader;
using Hector.Core.Support;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Hector.Core.Excel
{
    public class ExcelDataReaderAccessor : IExcelDataAccessor
    {
        public string FilePath { get; }

        public ExcelDataReaderAccessor(string filePath)
        {
            filePath.AssertHasText("filePath");
            FilePath = filePath;
        }

        public DataSet ReadData(string[] sheetNames)
        {
            using (var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();

                    List<string> tableNames = new List<string>();

                    for (var i = 0; i < result.Tables.Count; i++)
                    {
                        tableNames.Add(result.Tables[i].TableName);
                    }

                    foreach (string tableName in tableNames)
                    {
                        if (!sheetNames.IsNull() && !sheetNames.Contains(tableName))
                        {
                            result.Tables.Remove(tableName);
                        }
                    }

                    return result;
                }
            }
        }

        public DataSet ReadDataWithMixedData(string[] sheetNames)
        {
            using (var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet
                        (
                            new ExcelDataSetConfiguration()
                            {
                                UseColumnDataType = false,
                                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                                {
                                    UseHeaderRow = true
                                }
                            }
                        );

                    List<string> tableNames = new List<string>();

                    for (var i = 0; i < result.Tables.Count; i++)
                    {
                        tableNames.Add(result.Tables[i].TableName);
                    }

                    foreach (string tableName in tableNames)
                    {
                        if (!sheetNames.IsNull() && !sheetNames.Contains(tableName))
                        {
                            result.Tables.Remove(tableName);
                        }
                    }

                    return result;
                }
            }
        }

        public string[] ExtractSheetNames()
        {
            List<string> sheetNames = new List<string>();

            using (var stream = File.Open(FilePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    do
                    {
                        sheetNames.Add(reader.Name);
                    }
                    while (reader.NextResult());
                }
            }

            return sheetNames.ToArray();
        }
    }
}
