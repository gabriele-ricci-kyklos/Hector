using Hector.Core;
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

        public override int FieldsCount => _types.Length;

        public TupleDataRecordMapper(Type type, DataRecordMapperFactory mapperFactory)
            : base(type, mapperFactory)
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
            object?[] values = new object[FieldsCount];

            for (int i = 0; i < _types.Length; ++i)
            {
                values[i] = BuildObject(_types[i], records, ref position);
            }

            object tuple = _constructor.Invoke(values);
            return tuple;
        }
    }
}
