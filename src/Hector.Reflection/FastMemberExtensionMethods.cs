using FastMember;

namespace Hector.Reflection
{
    public static class FastMemberExtensionMethods
    {
        public static bool CanWrite(this Member m)
        {
            try
            {
                return m.CanWrite;
            }
            catch
            {
                return false;
            }
        }

        public static bool CanRead(this Member m)
        {
            try
            {
                return m.CanRead;
            }
            catch
            {
                return false;
            }
        }

        public static Member[] GetMemberList<T>(this T obj, string[]? propertiesToExclude = null)
            where T : notnull =>
            TypeAccessor
                .Create(obj.GetType())
                .GetMemberList(propertiesToExclude);

        public static Member[] GetMemberList(this Type type, string[]? propertiesToExclude = null) =>
            TypeAccessor
                .Create(type)
                .GetMemberList(propertiesToExclude);

        public static Member[] GetMemberList(this TypeAccessor typeAccessor, string[]? propertiesToExclude = null)
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

            return
                returnList
                    .OrderBy(x => x.Ordinal)
                    .ToArray();
        }

        public static Dictionary<string, object> GetMemberValues<T>(this T item, TypeAccessor? typeAccessor = null, Member[]? propertyList = null, string[]? propertiesToExclude = null)
        {
            TypeAccessor accessor = typeAccessor ?? TypeAccessor.Create(typeof(T));

            Member[] properties = propertyList.ToNullIfEmptyArray() ?? accessor.GetMemberList(propertiesToExclude);
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

        public static void SetMemberValue<T>(this T item, string propertyName, object value, TypeAccessor? typeAccessor = null)
        {
            TypeAccessor accessor = typeAccessor ?? TypeAccessor.Create(typeof(T));
            accessor[item, propertyName] = value;
        }
    }
}
