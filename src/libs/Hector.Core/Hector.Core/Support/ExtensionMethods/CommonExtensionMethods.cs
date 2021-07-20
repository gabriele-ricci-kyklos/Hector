using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Hector.Core.Support
{
    public static class CommonExtensionMethods
    {
        public static bool IsNull(this object o)
        {
            return ReferenceEquals(o, null);
        }

        public static bool IsNotNull(this object o)
        {
            return !o.IsNull();
        }

        public static T ConvertTo<T>(this object value)
        {
            Type t = typeof(T);
            t = Nullable.GetUnderlyingType(t) ?? t;

            T retValue = (value == null || DBNull.Value.Equals(value)) ? default(T) : (T)Convert.ChangeType(value, t);
            return retValue;
        }

        public static object ConvertTo(this object value, Type typeTo)
        {
            typeTo = Nullable.GetUnderlyingType(typeTo) ?? typeTo;

            if (value == null || DBNull.Value.Equals(value))
            {
                return null;
            }

            if (value.GetType() == typeTo)
            {
                return value;
            }
            object retValue = Convert.ChangeType(value, typeTo);
            return retValue;
        }

        public static TResult Return<TInput, TResult>(this TInput o, Func<TInput, TResult> evaluator, TResult failureValue) where TInput : class
        {
            return o == null ? failureValue : evaluator(o);
        }

        public static TResult Return<TInput, TResult>(this TInput? o, Func<TInput, TResult> evaluator, TResult failureValue)
            where TInput : struct
        {
            return !o.HasValue ? failureValue : evaluator(o.Value);
        }

        public static string XmlSerialize<T>(this T value)
        {
            XmlSerializer xsSubmit = new XmlSerializer(typeof(T));
            var xml = string.Empty;

            using (var sww = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sww))
                {
                    xsSubmit.Serialize(writer, value);
                    xml = sww.ToString();
                }
            }

            return xml;
        }

        public static string FormatAsCSV(this object item)
        {
            if (item.IsNull())
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            var properties = item.GetProperties();
            int i = 0;
            int count = properties.Count();

            foreach (PropertyInfo propInfo in properties)
            {
                string value = propInfo.GetValue(item, null).ToSafeString();

                if (value.IsNullOrEmpty())
                {
                }
                else if (value.Where(z => z == ',').Any())
                {
                    sb.Append($"\"{value}\"");
                }
                else
                {
                    sb.Append(value);
                }

                if (++i != count)
                {
                    sb.Append(",");
                }
            }

            return sb.ToString();
        }

        //credits: https://docs.microsoft.com/it-it/dotnet/csharp/programming-guide/nullable-types/how-to-identify-a-nullable-type
        public static bool IsNullable<T>(this T obj)
        {
            if (obj.IsNull())
            {
                return true;
            }

            Type type = typeof(T);
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static T? ToEnum<T>(this string s) where T : struct
        {
            if (s.IsNullOrBlankString())
            {
                return new T?();
            }

            if (Enum.TryParse<T>(s, out T result))
            {
                return result;
            }

            return new T?();
        }

        public static T ToEnum<T>(this string s, T failureValue) where T : struct
        {
            T? value = s.ToEnum<T>();
            return value ?? failureValue;
        }

        public static string GetFileName(this Uri uri)
        {
            uri.AssertNotNull(nameof(uri));
            return Path.GetFileName(uri.LocalPath);
        }

        public static Uri ToUri(this string uriString)
        {
            uriString.AssertHasText(nameof(uriString));

            if (Uri.TryCreate(uriString, UriKind.Absolute, out Uri uri))
            {
                return uri;
            }

            throw new UriFormatException($"The string '{uriString}' is not a valid URI");
        }
    }
}
