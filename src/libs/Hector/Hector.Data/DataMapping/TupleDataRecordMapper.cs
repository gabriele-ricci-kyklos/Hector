using Hector.Core;
using Hector.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hector.Data.DataMapping
{
    internal class TupleDataRecordMapper : BaseDataRecordMapper
    {
        private readonly ConstructorInfo _constructor;
        private readonly Type[] _types;
        private readonly Dictionary<Type, IDataRecordMapper> _mappers = [];

        public override int FieldsCount => _types.Length;

        public TupleDataRecordMapper(Type type, string[] dataRecordColumns)
            : base(type, dataRecordColumns)
        {
            _constructor =
                _type
                    .GetConstructors()
                    .First();

            _types =
                _constructor
                    .GetParameters()
                    .Select(x => x.ParameterType)
                    .ToArray();
        }

        public override object Build(int position, DataRecord[] records)
        {
            object?[] values = new object[_types.Length];

            for (int i = 0; i < _types.Length; ++i)
            {
                Type type = _types[i];
                IDataRecordMapper mapper = GetDataRecordMapper(type);
                object? dataObj = mapper.Build(position, records);
                position += mapper.FieldsCount;
                values[i] = dataObj;
            }

            object tuple = _constructor.Invoke(values);
            return tuple;
        }

        private IDataRecordMapper GetDataRecordMapper(Type type)
        {
            if (_mappers.TryGetValue(type, out IDataRecordMapper? mapper))
            {
                return mapper;
            }

            if (type.IsSimpleType() || type == typeof(byte[]) || type == typeof(char[]))
            {
                mapper = new SingleValueDataRecordMapper(type, _dataRecordColumns);
            }
            else if (type.IsTypeValueTuple() || type.IsTypeTuple() || type.IsTypeDictionary())
            {
                throw new NotSupportedException($"{type.FullName} cannot be nested inside a tuple");
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
