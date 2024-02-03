using System.Reflection;

namespace Hector.Core.Reflection
{
    public static class TypeExtensionMethods
    {
        public static object? GetDefaultValue(this Type t) =>
            typeof(TypeExtensionMethods).GetMethod(nameof(GetDefaultGeneric), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(t)?.Invoke(null, null);

        private static T? GetDefaultGeneric<T>() => default;
    }
}
