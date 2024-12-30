using Hector.Data.Entities.Attributes;
using Hector.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hector.Data.Entities
{
    public record EntityPropertyInfo(PropertyInfo PropertyInfo, PropertyDbType DbType, Type Type, string PropertyName, string ColumnName, short ColumnOrder,
        bool IsNullable, bool IsPrimaryKey, int MaxLength, int NumericPrecision, int NumericScale);

    public static class EntityHelper
    {
        public static EntityPropertyInfo[] GetEntityPropertyInfoList<T>() =>
            GetEntityPropertyInfoList(typeof(T));

        public static EntityPropertyInfo[] GetEntityPropertyInfoList(Type type)
        {
            PropertyInfo[] properties = type.GetPropertyInfoList();
            List<EntityPropertyInfo> results = [];

            for (int i = 0; i < properties.Length; ++i)
            {
                EntityPropertyInfoAttribute? attrib = properties[i].GetAttributeOfType<EntityPropertyInfoAttribute>(true);
                if (attrib is null)
                {
                    continue;
                }

                results
                    .Add
                    (
                        new EntityPropertyInfo
                        (
                            properties[i],
                            attrib.DbType,
                            properties[i].PropertyType,
                            properties[i].Name,
                            attrib.ColumnName ?? properties[i].Name,
                            attrib.ColumnOrder,
                            attrib.IsNullable,
                            attrib.IsPrimaryKey,
                            attrib.MaxLength,
                            attrib.NumericPrecision,
                            attrib.NumericScale
                        )
                    );
            }

            return results.ToArray();
        }

        public static string GetEntityTableName<T>(bool throwIfNotFound = true) => GetEntityTableName(typeof(T), throwIfNotFound);

        public static string GetEntityTableName(Type type, bool throwIfNotFound = true) =>
            type
                .GetAttributeOfType<EntityInfoAttribute>()
                ?.TableName
                ?? (throwIfNotFound ? throw new ArgumentNullException(nameof(EntityInfoAttribute.TableName)) : string.Empty);

        public static string[] GetPrimaryKeyFields(this Type entityType, EntityPropertyInfo[]? propertyInfoList = null)
        {
            if (entityType is null)
            {
                return [];
            }

            return
                (propertyInfoList ?? GetEntityPropertyInfoList(entityType))
                .Where(x => x.IsPrimaryKey)
                .OrderBy(x => x.ColumnOrder)
                .Select(x => x.ColumnName)
                .ToArray();
        }
    }
}
