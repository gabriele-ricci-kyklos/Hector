using FastMember;
using System;

namespace Hector.Data.Entities
{
    public class EntityDefinition<T>
        where T : IBaseEntity
    {
        public readonly Type Type;
        public readonly TypeAccessor TypeAccessor;
        public readonly EntityPropertyInfo[] PropertyInfoList;
        public readonly string TableName;
        public readonly string[] PrimaryKeyFields;

        public EntityDefinition()
        {
            Type = typeof(T);
            TypeAccessor = TypeAccessor.Create(Type);
            PropertyInfoList = EntityHelper.GetEntityPropertyInfoList<T>();
            TableName = EntityHelper.GetEntityTableName<T>();
            PrimaryKeyFields = EntityHelper.GetPrimaryKeyFields(Type, PropertyInfoList);
        }
    }
}
