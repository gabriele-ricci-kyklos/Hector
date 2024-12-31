using Hector.Reflection;
using System;
using System.Collections.Generic;

namespace Hector.Data.DataMapping
{
    internal class DataRecordMapperFactory
    {
        private readonly Dictionary<Type, IDataRecordMapper> _mappers = [];

        internal IDataRecordMapper GetDataRecordMapper<T>() =>
            GetDataRecordMapper(typeof(T));

        internal IDataRecordMapper GetDataRecordMapper(Type type)
        {
            if (_mappers.TryGetValue(type, out IDataRecordMapper? mapper))
            {
                return mapper;
            }

            if (type.IsSimpleType() || type == typeof(byte[]))
            {
                mapper = new SingleValueDataRecordMapper(type, this);
            }
            else if (type.IsTupleType() || type.IsValueTupleType())
            {
                mapper = new TupleDataRecordMapper(type, this);
            }
            else if (type.IsDictionaryType())
            {
                throw new NotSupportedException("A dictionary type is not supported");
            }
            else
            {
                mapper = new EntityDataRecordMapper(type, this);
            }

            _mappers.Add(type, mapper);
            return mapper;
        }
    }
}
