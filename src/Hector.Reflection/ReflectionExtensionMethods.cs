using FastMember;
using System.Reflection;

namespace Hector.Core.Reflection
{
    public static class ReflectionExtensionMethods
    {
        public static Type[] GetTypeHierarchy(this Type type)
        {
            List<Type> typesHierarchyList = [];
            Type? loopType = type;

            do
            {
                typesHierarchyList.Add(loopType);

                Member[]? baseTypeProperties =
                    type
                        .BaseType
                        ?.GetUnorderedPropertyList();

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

        public static Member[] GetUnorderedPropertyList(this Type type, string[]? propertiesToExclude = null) =>
            TypeAccessor
                .Create(type)
                .GetUnorderedPropertyList(propertiesToExclude);

        public static Member[] GetUnorderedPropertyList(this TypeAccessor typeAccessor, string[]? propertiesToExclude = null)
        {
            MemberSet members = typeAccessor.GetMembers();
            HashSet<string> propertiesToExcludeSet = new(propertiesToExclude.ToEmptyIfNull(), StringComparer.OrdinalIgnoreCase);
            List<Member> returnList = [];

            foreach (Member member in members)
            {
                if (propertiesToExcludeSet.Contains(member.Name))
                {
                    continue;
                }

                returnList.Add(member);
            }

            return returnList.ToArray();
        }

        public static Member[] GetOrderedPropertyList(this Type type, string[]? propertiesToExclude = null) =>
            TypeAccessor
                .Create(type)
                .GetOrderedPropertyList(type, propertiesToExclude);

        public static Member[] GetOrderedPropertyList(this TypeAccessor typeAccessor, Type type, string[]? propertiesToExclude = null)
        {
            Dictionary<string, Member> memberDict =
                typeAccessor
                    .GetMembers()
                    .ToDictionary(x => x.Name);

            Type[] typesHierarchyList = type.GetTypeHierarchy();

            Dictionary<int, List<Member>> baseTypePropList = [];
            List<Member> concreteTypePropList = [];

            foreach (PropertyInfo prop in type.GetPropertiesForType(propertiesToExclude.ToEmptyArrayIfNull()))
            {
                Member? member = memberDict.GetValueOrDefault(prop.Name);
                if (member is null)
                {
                    continue;
                }

                bool isBaseTypeProperty = prop.DeclaringType != prop.ReflectedType;
                if (isBaseTypeProperty)
                {
                    int typeHirearchyOrder = prop.DeclaringType is null ? -1 : Array.IndexOf(typesHierarchyList, prop.DeclaringType);
                    if (!baseTypePropList.TryAdd(typeHirearchyOrder, new(member.AsArray())))
                    {
                        baseTypePropList[typeHirearchyOrder].Add(member);
                    }
                }
                else
                {
                    concreteTypePropList.Add(member);
                }
            }

            return
                baseTypePropList
                    .OrderBy(x => x.Key)
                    .SelectMany(x => x.Value)
                    .Union(concreteTypePropList)
                    .ToArray();
        }

        public static Dictionary<string, object> GetPropertyValues<T>(this T item, TypeAccessor? typeAccessor = null, Member[]? propertyList = null, string[]? propertiesToExclude = null)
        {
            TypeAccessor accessor = typeAccessor ?? TypeAccessor.Create(typeof(T));

            Member[] properties = propertyList.ToNullIfEmptyArray() ?? accessor.GetUnorderedPropertyList(propertiesToExclude);
            HashSet<string> propertiesToExcludeSet = new(propertiesToExclude.ToEmptyIfNull(), StringComparer.OrdinalIgnoreCase);
            Dictionary<string, object> propertyValues = [];

            foreach (Member property in properties)
            {
                if (propertiesToExcludeSet.Contains(property.Name))
                {
                    continue;
                }

                object propertyValue = accessor[item, property.Name];
                propertyValues.Add(property.Name, propertyValue);
            }

            return propertyValues;
        }
    }
}
