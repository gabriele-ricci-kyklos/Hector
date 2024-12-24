using FastMember;

namespace Hector.Reflection
{
    public static class PropertiesComparer
    {
        public static bool CompareProperties<T, R>(T x, R y, string[]? orderedProperties = null, bool useUnderlyingTypeForNullables = true)
        {
            TypeAccessor TTypeAccessor = TypeAccessor.Create(typeof(T));
            TypeAccessor RTypeAccessor = TypeAccessor.Create(typeof(R));

            Dictionary<string, Member> TmemberDict =
                TTypeAccessor
                    .GetMemberList()
                    .ToDictionary(x => x.Name);

            Dictionary<string, Member> RmemberDict =
                RTypeAccessor
                    .GetMemberList()
                    .ToDictionary(x => x.Name);

            foreach (string property in orderedProperties.ToNullIfEmpty() ?? TmemberDict.Keys)
            {
                Member? TMember =
                    TmemberDict
                        .GetValueOrDefault(property)
                        .GetNonNullOrThrow();

                Member? RMember =
                    RmemberDict
                        .GetValueOrDefault(property)
                        .GetNonNullOrThrow();

                Type tMemberType = useUnderlyingTypeForNullables ? Nullable.GetUnderlyingType(TMember.Type) ?? TMember.Type : TMember.Type;
                Type rMemberType = useUnderlyingTypeForNullables ? Nullable.GetUnderlyingType(RMember.Type) ?? RMember.Type : TMember.Type;

                if (tMemberType != rMemberType)
                {
                    return false;
                }

                object xValue = TTypeAccessor[x, property];
                object yValue = RTypeAccessor[y, property];

                if (xValue is null && yValue is null)
                {
                    continue;
                }
                else if (xValue is null || yValue is null)
                {
                    return false;
                }

                bool areEqual =
                    xValue
                        .ConvertTo(TMember.Type)
                        ?.Equals(yValue.ConvertTo(RMember.Type))
                        ?? false;

                if (!areEqual)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
