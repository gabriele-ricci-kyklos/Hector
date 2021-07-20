using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hector.Core.Support
{
    public static class ReflectionExtensionMethods
    {
        public static IEnumerable<PropertyInfo> GetProperties(this object item)
        {
            item.AssertNotNull("item");

            foreach (PropertyInfo property in item.GetType().GetProperties())
            {
                yield return property;
            }
        }

        public static IEnumerable<NameValue<string, object>> GetPropertyValues(this object item, string[] propertiesToExclude = null)
        {
            foreach (PropertyInfo property in item.GetType().GetProperties())
            {
                PropertyInfo propertyInfo = property;
                if (propertiesToExclude == null || !propertiesToExclude.Contains(propertyInfo.Name))
                {
                    yield return new NameValue<string, object>(property.Name, () => propertyInfo.GetValue(item, null));
                }
            }
        }

        public static T GetSafePropertyValue<T>(this object item, string propertyName, bool alsoNonPublic = false, bool caseSensitive = true, T defaultValue = default(T))
        {
            object value = GetSafePropertyValue(item, propertyName, alsoNonPublic, caseSensitive);
            if (value == null)
            {
                return defaultValue;
            }
            return value.ConvertTo<T>();
        }

        public static object GetSafePropertyValue(this object item, string propertyName, bool alsoNonPublic = false, bool caseSensitive = true)
        {
            return GetPropertyValueImpl(item, propertyName, alsoNonPublic, caseSensitive, false);
        }

        public static object GetPropertyValue(this object item, string propertyName, bool alsoNonPublic = false, bool caseSensitive = true)
        {
            return GetPropertyValueImpl(item, propertyName, alsoNonPublic, caseSensitive, true);
        }

        private static object GetPropertyValueImpl(this object item, string propertyName, bool alsoNonPublic = false, bool caseSensitive = true, bool throwExIfNoPropFound = true)
        {
            item.AssertNotNull("item");

            BindingFlags bflags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
            if (alsoNonPublic)
            {
                bflags = bflags | BindingFlags.NonPublic;
            }

            PropertyInfo propInfo;

            if (caseSensitive)
            {
                propInfo = item.GetType().GetProperty(propertyName, bflags);
            }
            else
            {
                propInfo = item.GetType().GetProperties(bflags).FirstOrDefault(x => string.Equals(propertyName, x.Name, StringComparison.InvariantCultureIgnoreCase));
            }

            if (propInfo == null)
            {
                if (throwExIfNoPropFound)
                {
                    throw new ArgumentException("Property {0} does not exist or is inaccessible".FormatWith(propertyName));
                }
                else
                {
                    return null;
                }

            }
            return propInfo.GetValue(item, null);
        }

        public static bool SetPropertyValue(this object target, string propertyName, object value)
        {
            if (target == null)
            {
                return false;
            }
            var propInfo = target.GetType().GetProperty(propertyName);
            if (propInfo == null)
            {
                return false;
            }
            propInfo.SetValue(target, value, null);
            return true;
        }

        public static bool SetFieldValue(this object target, string propertyName, object value)
        {
            if (target == null)
            {
                return false;
            }
            var fieldInfo = target.GetType().GetField(propertyName);
            if (fieldInfo == null)
            {
                return false;
            }
            fieldInfo.SetValue(target, value);
            return true;
        }
    }
}
