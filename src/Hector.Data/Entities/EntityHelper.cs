using Hector.Core.Reflection;
using Hector.Data.Entities.Attributes;
using System;
using System.Reflection;

namespace Hector.Data.Entities
{
    public record EntityFieldInfo(EntityPropertyInfo PropertyInfo, string ParamName, object Value);
    public record EntityPropertyInfo(PropertyDbType DbType, Type Type, string PropertyName, string ColumnName, short Order);

    public static class EntityHelper
    {
        public static EntityPropertyInfo[] GetEntityPropertyInfoList(Type type)
        {
            PropertyInfo[] properties = type.GetPropertyInfoList(["TableName", "IsView", "Alias"]);
            EntityPropertyInfo[] results = new EntityPropertyInfo[properties.Length];

            for (int i = 0; i < properties.Length; ++i)
            {
                EntityPropertyInfoAttribute? attrib = properties[i].GetAttributeOfType<EntityPropertyInfoAttribute>(true);
                if(attrib is not null)
                {
                    results[i] =
                        new EntityPropertyInfo
                        (
                            attrib.DbType,
                            properties[i].PropertyType,
                            properties[i].Name,
                            attrib.ColumnName ?? properties[i].Name,
                            attrib.ColumnOrder
                        );
                }
            }

            return results;
        }
    }
}
