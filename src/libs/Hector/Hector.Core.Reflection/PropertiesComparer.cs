using FastMember;

namespace Hector.Core.Reflection
{
    public static class PropertiesComparer
    {
        public static bool CompareProperties<T, R>(T x, R y, string[] orderedProperties)
        {
            TypeAccessor TTypeAccessor = TypeAccessor.Create(typeof(T));
            TypeAccessor RTypeAccessor = TypeAccessor.Create(typeof(R));

            Dictionary<string, Member> TmemberDict =
                TTypeAccessor
                    .GetUnorderedPropertyList()
                    .ToDictionary(x => x.Name);

            Dictionary<string, Member> RmemberDict =
                RTypeAccessor
                    .GetUnorderedPropertyList()
                    .ToDictionary(x => x.Name);

            foreach (string property in orderedProperties)
            {
                Member? TMember =
                    TmemberDict
                        .GetValueOrDefault(property)
                        .GetNonNullOrThrow();

                Member? RMember =
                    RmemberDict
                        .GetValueOrDefault(property)
                        .GetNonNullOrThrow();

                if (TMember.Type != RMember.Type)
                {
                    return false;
                }

                object xValue = TTypeAccessor[x, property];
                object yValue = RTypeAccessor[y, property];

                if (xValue is null && yValue is null)
                {
                    return true;
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
