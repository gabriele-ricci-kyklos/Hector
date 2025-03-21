﻿using FastMember;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Hector.Reflection
{
    public static class ReflectionExtensionMethods
    {
        public static PropertyInfo[] GetPropertyInfoList(this Type type, params string[]? propertiesToExclude)
        {
            HashSet<string> propsToExclude = new(propertiesToExclude.ToEmptyArrayIfNull());
            PropertyInfo[] properties = type.GetProperties();
            return properties.Where(x => !propsToExclude.Contains(x.Name)).ToArray();
        }

        public static Member[] GetUnorderedPropertyList<T>(this T obj, string[]? propertiesToExclude = null)
            where T : notnull =>
            TypeAccessor
                .Create(obj.GetType())
                .GetUnorderedPropertyList(propertiesToExclude);

        public static Member[] GetUnorderedPropertyList(this Type type, string[]? propertiesToExclude = null) =>
            TypeAccessor
                .Create(type)
                .GetUnorderedPropertyList(propertiesToExclude);

        public static Member[] GetUnorderedPropertyList(this TypeAccessor typeAccessor, string[]? propertiesToExclude = null)
        {
            MemberSet members = typeAccessor.GetMembers();
            HashSet<string> propertiesToExcludeSet = new(propertiesToExclude.ToEmptyIfNull(), StringComparer.OrdinalIgnoreCase);
            return members.Where(x => !propertiesToExcludeSet.Contains(x.Name)).ToArray();
        }

        public static Type[] GetTypeHierarchy(this Type type)
        {
            List<Type> typesHierarchyList = [];
            Type? loopType = type;

            do
            {
                typesHierarchyList.Add(loopType);
                loopType = loopType.BaseType;
            }
            while (loopType is not null && loopType != typeof(object));

            typesHierarchyList.Reverse();

            return typesHierarchyList.ToArray();
        }

        public static PropertyInfo[] GetHierarchicalOrderedPropertyList(this Type type, string[]? propertiesToExclude = null)
        {
            Type[] typesHierarchyList = type.GetTypeHierarchy();

            Dictionary<int, List<PropertyInfo>> baseTypePropList = [];
            List<PropertyInfo> concreteTypePropList = [];

            foreach (PropertyInfo prop in type.GetPropertyInfoList(propertiesToExclude.ToEmptyArrayIfNull()))
            {
                bool isBaseTypeProperty = prop.DeclaringType != prop.ReflectedType;
                if (isBaseTypeProperty)
                {
                    int typeHirearchyOrder = prop.DeclaringType is null ? -1 : Array.IndexOf(typesHierarchyList, prop.DeclaringType);
                    if (!baseTypePropList.NetCoreTryAdd(typeHirearchyOrder, new(prop.AsArray())))
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

        public static Dictionary<string, object?> GetPropertyValues(this object item, PropertyInfo[]? propertyList = null, string[]? propertiesToExclude = null)
        {
            PropertyInfo[] properties = propertyList.ToNullIfEmptyArray() ?? item.GetType().GetHierarchicalOrderedPropertyList(propertiesToExclude);
            HashSet<string> propertiesToExcludeSet = new(propertiesToExclude.ToEmptyIfNull(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, object?> propertyValues = [];

            foreach (PropertyInfo property in properties)
            {
                if (propertiesToExcludeSet.Contains(property.Name))
                {
                    continue;
                }

                object? propertyValue = property.GetValue(item, null);
                propertyValues.Add(property.Name, propertyValue);
            }

            return propertyValues;
        }

        public static bool HasAttribute<TAttrib>(this Type type) where TAttrib : Attribute =>
            HasAttribute(type, typeof(TAttrib));

        public static bool HasAttribute(this Type type, Type attributeType) =>
            type.IsDefined(attributeType, true);

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

        public static IEnumerable<T> ToEntityList<T>(this IEnumerable<DataRow> rows, bool throwIfPropertyNotFound = false, IEqualityComparer<string>? propertyNameComparer = null, Dictionary<Type, Func<object, object>>? typesMap = null, Dictionary<string, string>? propertyNamesMap = null)
            where T : new()
        {
            foreach (DataRow dr in rows)
            {
                T newRow = dr.ToEntity<T>(throwIfPropertyNotFound, propertyNameComparer ?? StringComparer.OrdinalIgnoreCase, typesMap, propertyNamesMap);
                yield return newRow;
            }
        }

        public static IEnumerable<T> ToEntityList<T>(this DataTable table, bool throwIfPropertyNotFound = false, IEqualityComparer<string>? propertyNameComparer = null, Dictionary<Type, Func<object, object>>? typesMap = null, Dictionary<string, string>? propertyNamesMap = null)
            where T : new() =>
            table
                .Rows
                .Cast<DataRow>()
                .ToEntityList<T>(throwIfPropertyNotFound, propertyNameComparer ?? StringComparer.OrdinalIgnoreCase, typesMap, propertyNamesMap);

        public static T ToEntity<T>(this DataRow tableRow, bool throwIfPropertyNotFound, IEqualityComparer<string> propertyNameComparer, Dictionary<Type, Func<object, object>>? typesMap, Dictionary<string, string>? propertyNamesMap)
        {
            Type type = typeof(T);

            T returnObj =
                (T)Activator
                    .CreateInstance(type)
                    .GetNonNullOrThrow(nameof(returnObj));

            TypeAccessor typeAccessor = TypeAccessor.Create(type);
            Dictionary<string, Member> propertiesDict =
                typeAccessor
                    .GetMemberList()
                    .ToDictionary(x => x.Name, propertyNameComparer);

            foreach (DataColumn col in tableRow.Table.Columns)
            {
                string? mappedPropertyName = propertyNamesMap?.NetCoreGetValueOrDefault(col.ColumnName);
                Member? member = propertiesDict.NetCoreGetValueOrDefault(mappedPropertyName ?? col.ColumnName);
                if (member is null)
                {
                    if (throwIfPropertyNotFound)
                    {
                        throw new ArgumentException($"The property '{col.ColumnName}' has not been found");
                    }

                    continue;
                }

                Func<object, Type, object> cellConverter =
                    typesMap is null
                    ? (obj, t) => obj.ConvertTo(t)
                    : (obj, t) => typesMap[t];

                object value = cellConverter(tableRow[col], member.Type);
                returnObj.SetMemberValue(member.Name, value);
            }

            return returnObj;
        }

        public static void CopyPropertyValues(this object? src, object? dst, TypeAccessor? srcTypeAccessor = null, TypeAccessor? dstTypeAccessor = null, string[]? propertiesToExclude = null)
        {
            if (src is null || dst is null)
            {
                return;
            }

            TypeAccessor srcAcc = srcTypeAccessor ?? TypeAccessor.Create(src.GetType());
            TypeAccessor dstAcc = dstTypeAccessor ?? TypeAccessor.Create(dst.GetType());

            Dictionary<string, Member> dstPropsDict =
                dstAcc
                    .GetMemberList(propertiesToExclude)
                    .ToDictionary(x => x.Name, StringComparer.InvariantCultureIgnoreCase);

            Member[] props = srcAcc.GetMemberList(propertiesToExclude);
            foreach (Member p in props)
            {
                Member? dstProp = dstPropsDict.NetCoreGetValueOrDefault(p.Name);

                if (!p.CanWrite() || !p.CanRead() || dstProp is null)
                {
                    continue;
                }

                object? value = srcAcc[src, p.Name];

                if (dstProp.Type == typeof(string) && value is not null)
                {
                    value = value.ToString();
                }

                if (p.Type == typeof(string))
                {
                    value = value?.ConvertTo(dstProp.Type);
                }

                dstAcc[dst, p.Name] = value ?? p.Type.GetDefaultValue();
            }
        }
    }
}
