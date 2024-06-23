using FastMember;
using Hector.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hector.Data.DataMapping
{
    internal class EntityDataRecordMapper : BaseDataRecordMapper
    {
        private readonly TypeAccessor _typeAccessor;
        private readonly HashSet<string> _propertiesSet;

        public override int FieldsCount => _propertiesSet.Count;

        public EntityDataRecordMapper(Type type, string[] dataRecordColumns)
            : base(type, dataRecordColumns)
        {
            _typeAccessor =
                TypeAccessor
                    .Create(_type, true);

            _propertiesSet =
                _typeAccessor
                    .GetUnorderedPropertyList()
                    .Select(x => x.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public override object Build(int position, DataRecord[] records)
        {
            object resultObj =
                Activator.CreateInstance(_type)
                ?? throw new InvalidOperationException("Unable to instantiate the object");

            for (int i = 0; i < records.Length; ++i)
            {
                if (!_propertiesSet.TryGetValue(records[i].Name, out string? propertyName))
                {
                    continue;
                }

                _typeAccessor[resultObj, propertyName] = records[i].Value;
            }

            return resultObj;
        }
    }
}
