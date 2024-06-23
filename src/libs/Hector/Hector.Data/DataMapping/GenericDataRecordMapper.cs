using Hector.Core;
using Hector.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Data;

namespace Hector.Data.DataMapping
{
    public class DataRecord
    {
        public string Name { get; set; }
        public object RawValue { get; set; }
        public object? Value { get; set; }

        public DataRecord(string name, object value)
        {
            Name = name;
            RawValue = value;
            Value = value == DBNull.Value ? null : value;
        }
    }

    internal class GenericDataRecordMapper<T>
    {
        private string[] _dataRecordColumns = [];
        private readonly Dictionary<Type, IDataRecordMapper> _mappers = [];

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

            IDataRecordMapper mapper = GetDataRecordMapper();

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

        private IDataRecordMapper GetDataRecordMapper()
        {
            Type type = typeof(T);

            if(_mappers.TryGetValue(type, out IDataRecordMapper? mapper))
            {
                return mapper;
            }

            if (type.IsSimpleType() || type == typeof(byte[]))
            {
                mapper = new SingleValueDataRecordMapper(type, _dataRecordColumns);
            }
            else if (type.IsTypeTuple() || type.IsTypeValueTuple())
            {
                mapper = new TupleDataRecordMapper(type, _dataRecordColumns);
            }
            else if (type.IsTypeDictionary())
            {
                throw new NotSupportedException("A dictionary type is not supported");
            }
            else
            {
                mapper = new EntityDataRecordMapper(type, _dataRecordColumns);
            }

            _mappers.Add(type, mapper);
            return mapper;
        }
    }
}
