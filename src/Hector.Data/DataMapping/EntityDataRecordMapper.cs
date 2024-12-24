using FastMember;
using Hector.Reflection;
using Hector.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hector.Data.DataMapping
{
    internal class EntityDataRecordMapper : BaseDataRecordMapper
    {
        private readonly TypeAccessor _typeAccessor;
        private readonly Dictionary<string, string> _propertiesMapping;
        private readonly ObjectConstructor _typeConstructorDelegate;

        public override int FieldsCount => _propertiesMapping.Count;

        public EntityDataRecordMapper(Type type, DataRecordMapperFactory mapperFactory)
            : base(type, mapperFactory)
        {
            _typeAccessor =
                TypeAccessor
                    .Create(_type, true);

            _propertiesMapping =
                EntityHelper
                    .GetEntityPropertyInfoList(_type)
                    .ToDictionary(x => x.ColumnName, x => x.PropertyName);

            _typeConstructorDelegate = ObjectActivator.CreateILConstructorDelegate(_type);
        }

        public override object Build(int position, DataRecord[] records)
        {
            object resultObj = _typeConstructorDelegate();

            for (int i = 0; i < records.Length; ++i)
            {
                if (!_propertiesMapping.TryGetValue(records[i].Name, out string? propertyName))
                {
                    continue;
                }

                _typeAccessor[resultObj, propertyName] = records[i].Value;
            }

            return resultObj;
        }
    }
}
