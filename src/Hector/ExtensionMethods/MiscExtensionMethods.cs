using System;

namespace Hector
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

        public static bool TryConvertTo<T>(this object value, out T? convertedValue)
        {
            convertedValue = default;

            if (TryConvertTo(value, typeof(T), out object? convertedRawValue))
            {
                convertedValue = (T)convertedRawValue!;
                return true;
            }

            return false;
        }

        public static bool TryConvertTo(this object value, Type typeTo, out object? convertedValue)
        {
            convertedValue = default;
            typeTo = Nullable.GetUnderlyingType(typeTo) ?? typeTo;

            if (value is null || DBNull.Value.Equals(value))
            {
                return false;
            }

            if (value.GetType() == typeTo)
            {
                convertedValue = value;
            }
            else
            {
                try
                {
                    convertedValue = Convert.ChangeType(value, typeTo);
                }
                catch
                {
                    return false;
                }
            }

            return true;
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
