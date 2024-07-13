using FastMember;
using Hector.Core;
using Hector.Core.Reflection;
using Hector.Data.Entities.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hector.Data.DataMapping
{
    record EntityPropertyInfo(Type Type, string PropertyName, string ColumnName);

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
                GetEntityPropertyInfoList()
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

        private EntityPropertyInfo[] GetEntityPropertyInfoList()
        {
            PropertyInfo[] properties = _type.GetPropertyInfoList();
            EntityPropertyInfo[] results = new EntityPropertyInfo[properties.Length];

            for (int i = 0; i < properties.Length; ++i)
            {
                EntityPropertyInfoAttribute? attrib = properties[i].GetAttributeOfType<EntityPropertyInfoAttribute>(true);
                results[i] = new EntityPropertyInfo(properties[i].PropertyType, properties[i].Name, attrib?.ColumnName ?? properties[i].Name);
            }

            return results.ToArray();
        }
    }
}
