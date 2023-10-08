using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hector.Core
{
    public static class ReflectionExtensionMethods
    {
        public static IEnumerable<PropertyInfo> GetPropertiesForType(this Type type, params string[]? propertiesToExclude)
        {
            HashSet<string> propsToExclude = new(propertiesToExclude.ToEmptyArrayIfNull());
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (!propsToExclude.Contains(propertyInfo.Name))
                {
                    yield return propertyInfo;
                }
            }
        }

        public static IEnumerable<(string Name, Lazy<object> Value)> GetLazyPropertyValues(this object item, string[]? propertiesToExclude = null)
        {
            foreach (PropertyInfo property in item.GetType().GetPropertiesForType(propertiesToExclude))
            {
                yield return (property.Name, new(() => property.GetValue(item, null)));
            }
        }

        public static Type[] GetTypeHierarchy(this Type type)
        {
            List<Type> typesHierarchyList = new();
            Type? loopType = type;

            do
            {
                typesHierarchyList.Add(loopType);

                string[]? baseTypeProperties =
                    type
                        .BaseType
                        ?.GetPropertiesForType()
                        ?.Select(x => x.Name)
                        ?.ToArray();

                if (baseTypeProperties is null)
                {
                    break;
                }

                loopType = loopType.BaseType;
            }
            while (loopType is not null && loopType != typeof(object));

            typesHierarchyList.Reverse();

            return typesHierarchyList.ToArray();
        }

        public static PropertyInfo[] GetOrderedPropertyList(this Type type, string[]? propertiesToExclude = null)
        {
            Type[] typesHierarchyList = type.GetTypeHierarchy();

            Dictionary<int, List<PropertyInfo>> baseTypePropList = new();
            List<PropertyInfo> concreteTypePropList = new();

            foreach (PropertyInfo prop in type.GetPropertiesForType(propertiesToExclude.ToEmptyArrayIfNull()))
            {
                bool isBaseTypeProperty = prop.DeclaringType != prop.ReflectedType;
                if (isBaseTypeProperty)
                {
                    int typeHirearchyOrder = prop.DeclaringType is null ? -1 : Array.IndexOf(typesHierarchyList, prop.DeclaringType);
                    if (!baseTypePropList.TryAdd(typeHirearchyOrder, new(prop.AsArray())))
                    {
                        baseTypePropList[typeHirearchyOrder].Add(prop);
                    }
                }
                else
                {
                    concreteTypePropList.Add(prop);
                }
            }

            return
                baseTypePropList
                    .OrderBy(x => x.Key)
                    .SelectMany(x => x.Value)
                    .Union(concreteTypePropList)
                    .ToArray();
        }

        public static bool SetPropertyValue(this object target, string propertyName, object value)
        {
            if (target == null)
            {
                return false;
            }
            PropertyInfo propInfo = target.GetType().GetProperty(propertyName);
            if (propInfo == null)
            {
                return false;
            }
            propInfo.SetValue(target, value, null);
            return true;
        }

        public static bool SetFieldValue(this object target, string propertyName, object value)
        {
            if (target == null)
            {
                return false;
            }
            FieldInfo fieldInfo = target.GetType().GetField(propertyName);
            if (fieldInfo == null)
            {
                return false;
            }
            fieldInfo.SetValue(target, value);
            return true;
        }
    }
}
