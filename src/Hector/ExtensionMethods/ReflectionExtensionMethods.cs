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

        public static IEnumerable<(string Name, Lazy<object?> Value)> GetLazyPropertyValues(this object item, string[]? propertiesToExclude = null)
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
            if (target is null)
            {
                return false;
            }
            PropertyInfo? propInfo = target.GetType().GetProperty(propertyName);
            if (propInfo is null)
            {
                return false;
            }
            propInfo.SetValue(target, value, null);
            return true;
        }

        public static bool SetFieldValue(this object target, string propertyName, object value)
        {
            if (target is null)
            {
                return false;
            }
            FieldInfo? fieldInfo = target.GetType().GetField(propertyName);
            if (fieldInfo is null)
            {
                return false;
            }
            fieldInfo.SetValue(target, value);
            return true;
        }

        public static bool IsSimpleType(this Type type)
        {
            if (!(type == typeof(string)) && !type.GetNonNullableType().IsPrimitive && !type.GetNonNullableType().IsNumericType() && !(type.GetNonNullableType() == typeof(DateTime)))
            {
                return type.IsEnum;
            }

            return true;
        }

        public static Type GetNonNullableType(this Type type)
        {
            if (type.IsNullableType())
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        public static bool IsNullableType(this Type? type)
        {
            if (type is object && type!.IsGenericType)
            {
                return type!.GetGenericTypeDefinition() == typeof(Nullable<>);
            }

            return false;
        }

        public static bool IsNumericType(this Type? type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            return ((uint)(typeCode - 4) <= 11u);
        }

        public static bool TypeIsTuple(this Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (!genericTypeDefinition.Equals(typeof(Tuple<>)) && !genericTypeDefinition.Equals(typeof(Tuple<,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,,,,>)))
            {
                if (genericTypeDefinition.Equals(typeof(Tuple<,,,,,,,>)))
                {
                    return type.GetGenericArguments()[7].TypeIsTuple();
                }

                return false;
            }

            return true;
        }

        public static bool TypeIsValueTuple(this Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (!genericTypeDefinition.Equals(typeof(ValueTuple<>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,,,,>)))
            {
                if (genericTypeDefinition.Equals(typeof(ValueTuple<,,,,,,,>)))
                {
                    return type.GetGenericArguments()[7].TypeIsValueTuple();
                }

                return false;
            }

            return true;
        }

        public static bool TypeIsDictionary(this Type type)
        {
            return typeof(IDictionary<string, object>).IsAssignableFrom(type);
        }

        public static bool HasAttribute<TAttrib>(this Type type) where TAttrib : Attribute =>
            type.GetAttributeOfType<TAttrib>() is not null;

        public static TAttrib? GetAttributeOfType<TAttrib>(this Type type)
            where TAttrib : Attribute =>
            type.GetAttributeOfType<TAttrib>(inherit: false);

        public static TAttrib? GetAttributeOfType<TAttrib>(this Type type, bool inherit)
            where TAttrib : Attribute =>
            type.GetCustomAttributes(typeof(TAttrib), inherit).OfType<TAttrib>().FirstOrDefault();

        public static TAttrib? GetAttributeOfType<TAttrib>(this PropertyInfo propertyInfo, bool inherit)
            where TAttrib : Attribute =>
            GetAttributeOfTypeImpl<TAttrib>(propertyInfo, inherit);

        public static bool HasAttribute<TAttrib>(this PropertyInfo propertyInfo, bool inherit) where TAttrib : Attribute => propertyInfo.GetAttributeOfType<TAttrib>(inherit) is not null;

        public static TAttrib? GetAttributeOfType<TAttrib>(this FieldInfo fieldInfo, bool inherit)
            where TAttrib : Attribute =>
            GetAttributeOfTypeImpl<TAttrib>(fieldInfo, inherit);

        private static TAttrib? GetAttributeOfTypeImpl<TAttrib>(this MemberInfo memberInfo, bool inherit)
            where TAttrib : Attribute =>
            memberInfo.GetCustomAttributes(inherit).OfType<TAttrib>().FirstOrDefault();

        public static TAttrib[] GetAllAttributesOfType<TAttrib>(this Type type)
            where TAttrib : Attribute =>
            type.GetAllAttributesOfType<TAttrib>(inherit: false);

        public static TAttrib[] GetAllAttributesOfType<TAttrib>(this Type type, bool inherit)
            where TAttrib : Attribute =>
            type.GetCustomAttributes(typeof(TAttrib), inherit).OfType<TAttrib>().ToArray();
    }
}
