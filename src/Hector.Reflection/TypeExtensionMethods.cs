using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Hector.Reflection
{
    public static class TypeExtensionMethods
    {
        public static object? GetDefaultValue(this Type t) =>
            typeof(TypeExtensionMethods).GetMethod(nameof(GetDefaultGeneric), BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(t)?.Invoke(null, null);

        private static T? GetDefaultGeneric<T>() => default;

        public static bool IsSimpleType(this Type type)
        {
            if (!(type == typeof(string)) && !type.GetNonNullableType().IsPrimitive && !type.GetNonNullableType().IsNumericType() && !(type.GetNonNullableType() == typeof(DateTime)))
            {
                return type.IsEnum;
            }

            return true;
        }

        public static Type GetNonNullableType(this Type type)
        {
            if (type.IsNullableType())
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        public static bool IsNullableType(this Type? type)
        {
            if (type is not null && type!.IsGenericType)
            {
                return type!.GetGenericTypeDefinition() == typeof(Nullable<>);
            }

            return false;
        }

        public static bool IsNumericType(this Type? type)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            return ((uint)(typeCode - 4) <= 11u);
        }

        public static bool IsTupleType(this Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (!genericTypeDefinition.Equals(typeof(Tuple<>)) && !genericTypeDefinition.Equals(typeof(Tuple<,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,,,>)) && !genericTypeDefinition.Equals(typeof(Tuple<,,,,,,>)))
            {
                if (genericTypeDefinition.Equals(typeof(Tuple<,,,,,,,>)))
                {
                    return type.GetGenericArguments()[7].IsTupleType();
                }

                return false;
            }

            return true;
        }

        public static bool IsValueTupleType(this Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (!genericTypeDefinition.Equals(typeof(ValueTuple<>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,,,>)) && !genericTypeDefinition.Equals(typeof(ValueTuple<,,,,,,>)))
            {
                if (genericTypeDefinition.Equals(typeof(ValueTuple<,,,,,,,>)))
                {
                    return type.GetGenericArguments()[7].IsValueTupleType();
                }

                return false;
            }

            return true;
        }

        private static bool IsTupleOfSimpleTypes(this Type type)
        {
            Type[] typeArguments = type.GenericTypeArguments;

            return
                typeArguments.All(x => x.IsSimpleType())
                && type.IsTupleType();
        }

        private static bool IsValueTupleOfSimpleTypes(this Type type)
        {
            Type[] typeArguments = type.GenericTypeArguments;

            return
                typeArguments.All(x => x.IsSimpleType())
                && type.IsValueTupleType();
        }

        private static bool IsTupleOfMixedTypes(this Type type)
        {
            Type[] typeArguments = type.GenericTypeArguments;

            return
                type.IsTupleType()
                && typeArguments.Any(x => x.IsSimpleType())
                && typeArguments.Any(x => !x.IsSimpleType());
        }

        private static bool IsValueTupleOfMixedTypes(Type type)
        {
            Type[] typeArguments = type.GenericTypeArguments;

            return
                type.IsValueTupleType()
                && typeArguments.Any(x => x.IsSimpleType())
                && typeArguments.Any(x => !x.IsSimpleType());
        }

        private static bool IsTupleOrValueTupleOfComplexTypes(Type type)
        {
            Type[] typeArguments = type.GenericTypeArguments;

            return
                (type.IsTupleType() || type.IsValueTupleType())
                && typeArguments.All(x => !x.IsSimpleType());
        }

        public static bool IsDictionaryType(this Type type) =>
            type.IsDerivedType<IDictionary>();

        public static bool IsDerivedType<T>(this Type type) =>
            typeof(T).IsAssignableFrom(type);

        public static bool IsConcreteType(this Type type) =>
            type is not null
                && !type.IsAbstract
                && !type.IsArray
                && type != typeof(object)
                && !type.IsDerivedType<Delegate>();
    }
}
