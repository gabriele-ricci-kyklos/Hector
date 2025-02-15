﻿using System;
using System.Data;

namespace Hector.Data.DataMapping
{
    internal class DataRecord
    {
        internal string Name { get; set; }
        internal object RawValue { get; set; }
        internal object? Value { get; set; }

        internal DataRecord(string name, object value)
        {
            Name = name;
            RawValue = value;
            Value = value == DBNull.Value ? null : value;
        }
    }

    internal class GenericDataRecordMapper<T>
    {
        private string[] _dataRecordColumns = [];
        private readonly DataRecordMapperFactory mapperFactory = new();
        private readonly Type _type = typeof(T);

        internal T Build(IDataRecord dataRecord)
        {
            object[] values = new object[dataRecord.FieldCount];
            int copiedRecords = dataRecord.GetValues(values);
            if (copiedRecords != dataRecord.FieldCount)
            {
                throw new InvalidOperationException("Unable to read values");
            }

            if (_dataRecordColumns.Length == 0)
            {
                _dataRecordColumns = GetDataRecordColumnNames(dataRecord);
            }

            DataRecord[] records = new DataRecord[dataRecord.FieldCount];
            for (int i = 0; i < dataRecord.FieldCount; ++i)
            {
                string name = _dataRecordColumns[i];
                records[i] = new(name, values[i]);
            }

            IDataRecordMapper mapper = mapperFactory.GetDataRecordMapper(_type);

            object resultObj =
                mapper.Build(0, records)
                ?? throw new InvalidOperationException("Unable to create the object containing data");

            return resultObj.ConvertTo<T>();
        }

        private static string[] GetDataRecordColumnNames(IDataRecord dataRecord)
        {
            string[] dataRecordColumns = new string[dataRecord.FieldCount];
            for (int i = 0; i < dataRecord.FieldCount; ++i)
            {
                string name = dataRecord.GetName(i);
                dataRecordColumns[i] = name;
            }

            return dataRecordColumns;
        }
    }
}
