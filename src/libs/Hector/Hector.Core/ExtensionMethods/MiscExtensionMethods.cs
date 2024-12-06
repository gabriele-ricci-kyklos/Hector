using System;

namespace Hector.Core
{
    public static class MiscExtensionMethods
    {
        public static T ConvertTo<T>(this object value) =>
            (T)value.ConvertTo(typeof(T));

        public static object ConvertTo(this object value, Type typeTo)
        {
            typeTo = Nullable.GetUnderlyingType(typeTo) ?? typeTo;

            if (value == null || DBNull.Value.Equals(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.GetType() == typeTo)
            {
                return value;
            }

            object retValue = Convert.ChangeType(value, typeTo);
            return retValue;
        }

        public static T? ToEnum<T>(this string s, T? failureValue = default)
            where T : struct
        {
            if (s.IsNullOrBlankString()
                || !Enum.TryParse(s, out T result))
            {
                return failureValue ?? null;
            }

            return result;
        }
    }
}
