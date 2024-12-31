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
        public static EntityPropertyInfo[] GetEntityPropertyInfoList<T>() where T : IBaseEntity =>
            GetEntityPropertyInfoList(typeof(T));

        public static EntityPropertyInfo[] GetEntityPropertyInfoList(Type type)
        {
            if (!type.IsDerivedType<IBaseEntity>())
            {
                throw new NotSupportedException($"The type {type.FullName} is not an {nameof(IBaseEntity)}");
            }

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

        public static IEnumerable<Type> GetAllEntityTypesInAssemblyList
        (
            this IEnumerable<Assembly> assemblies,
            Func<Type, EntityInfoAttribute, bool>? predicate = null
        )
        {
            predicate ??= (t, eo) => true;

            var entityTypes =
                assemblies
                .SelectMany
                (x =>
                    x
                        .GetTypes()
                        .AsParallel()
                        .Where(x => x.IsDerivedType<IBaseEntity>() && x.IsConcreteType())
                        .Select(x => (EntityType: x, EntityInfo: x.GetAttributeOfType<EntityInfoAttribute>()))
                        .Where(x => x.EntityInfo is not null)
                        .Select
                        (
                            x =>
                                new
                                {
                                    Entity = x.EntityType,
                                    x.EntityInfo,
                                }
                        )
                        .Where(x => predicate(x.Entity, x.EntityInfo!))
                        .Select(x => x.Entity)
                );

            return entityTypes;
        }
    }
}
