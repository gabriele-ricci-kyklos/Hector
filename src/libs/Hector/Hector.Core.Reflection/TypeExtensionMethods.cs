using System.Reflection;

namespace Hector.Core.Reflection
{
    public static class TypeExtensionMethods
    {
        public static object? GetDefaultValue(this Type t) =>
            typeof(TypeExtensionMethods).GetMethod(nameof(GetDefaultGeneric), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(t)?.Invoke(null, null);

        private static T? GetDefaultGeneric<T>() => default;

        private static bool TypeIsTupleOfSimpleTypes(this Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                typeArguments.All(x => x.IsSimpleType())
                && type.IsTypeTuple();
        }

        private static bool TypeIsValueTupleOfSimpleTypes(this Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                typeArguments.All(x => x.IsSimpleType())
                && type.IsTypeValueTuple();
        }

        private static bool TypeIsTupleOfMixedTypes(this Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                type.IsTypeTuple()
                && typeArguments.Any(x => x.IsSimpleType())
                && typeArguments.Any(x => !x.IsSimpleType());
        }

        private static bool TypeIsValueTupleOfMixedTypes(Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                type.IsTypeValueTuple()
                && typeArguments.Any(x => x.IsSimpleType())
                && typeArguments.Any(x => !x.IsSimpleType());
        }

        private static bool TypeIsTupleOrValueTupleOfComplexTypes(Type type)
        {
            var typeArguments = type.GenericTypeArguments;

            return
                (type.IsTypeTuple() || type.IsTypeValueTuple())
                && typeArguments.All(x => !x.IsSimpleType());
        }

        public static bool IsTypeDictionary(this Type type) =>
            typeof(IDictionary<string, object>).IsAssignableFrom(type);
    }
}
