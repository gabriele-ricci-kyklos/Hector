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

        public static T? ToEnum<T>(this string s, bool ignoreCase = false, T? failureValue = default)
            where T : struct
        {
            if (!s.TryParseToEnum(out T result, ignoreCase))
            {
                return failureValue ?? null;
            }

            return result;
        }

        public static bool TryParseToEnum<T>(this string s, out T value, bool ignoreCase = false)
            where T : struct
        {
            value = default;

            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }

            return Enum.TryParse(s, ignoreCase, out value);
        }
    }
}
