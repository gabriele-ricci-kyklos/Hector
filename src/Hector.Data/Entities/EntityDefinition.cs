using FastMember;
using Hector.Reflection;
using System;

namespace Hector.Data.Entities
{
    public class EntityDefinition<T>() : EntityDefinition(typeof(T))
        where T : IBaseEntity
    {
    }

    public class EntityDefinition
    {
        public readonly Type Type;
        public readonly TypeAccessor TypeAccessor;
        public readonly EntityPropertyInfo[] PropertyInfoList;
        public readonly string TableName;
        public readonly string[] PrimaryKeyFields;

        public EntityDefinition(Type type)
        {
            if (!type.IsDerivedType<IBaseEntity>())
            {
                throw new NotSupportedException($"The type {type.FullName} is not an {nameof(IBaseEntity)}");
            }

            Type = type;
            TypeAccessor = TypeAccessor.Create(type);
            PropertyInfoList = EntityHelper.GetEntityPropertyInfoList(type);
            TableName = EntityHelper.GetEntityTableName(type);
            PrimaryKeyFields = EntityHelper.GetPrimaryKeyFields(type, PropertyInfoList);
        }
    }
}
