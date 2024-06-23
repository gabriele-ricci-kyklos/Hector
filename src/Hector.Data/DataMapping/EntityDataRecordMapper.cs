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
    record EntityPropertyInfo(string PropertyName, string ColumnName);

    internal class EntityDataRecordMapper : BaseDataRecordMapper
    {
        private readonly TypeAccessor _typeAccessor;
        private readonly Dictionary<string, string> _propertiesMapping;
        private readonly ObjectConstructor _typeConstructorDelegate;

        public override int FieldsCount => _propertiesMapping.Count;

        public EntityDataRecordMapper(Type type, string[] dataRecordColumns)
            : base(type, dataRecordColumns)
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
            PropertyInfo[] properties = _type.GetPropertiesForType();
            EntityPropertyInfo[] results = new EntityPropertyInfo[properties.Length];

            for (int i = 0; i < properties.Length; ++i)
            {
                string columnName = properties[i].Name;

                EntityPropertyInfoAttribute? attrib = properties[i].GetAttributeOfType<EntityPropertyInfoAttribute>(true);

                if (attrib is not null)
                {
                    columnName = attrib.ColumnName;
                }

                results[i] = new EntityPropertyInfo(properties[i].Name, columnName);
            }

            return results.ToArray();
        }
    }
}
