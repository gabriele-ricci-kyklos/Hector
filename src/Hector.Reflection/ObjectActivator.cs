using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Hector.Reflection
{
    //credits: https://stackoverflow.com/questions/4432026/activator-createinstance-performance-alternative
    //credits: https://stackoverflow.com/questions/26477540/creating-a-dynamicmethod-to-invoke-a-constructor

    public delegate object ObjectConstructor();

    public class ObjectActivator
    {
        public static T CreateInstanceIL<T>() =>
            (T)CreateILConstructorDelegate<T>()();

        public static object CreateInstanceIL(Type type) =>
            CreateILConstructorDelegate(type)();

        public static ObjectConstructor CreateILConstructorDelegate<T>() =>
            CreateILConstructorDelegate(typeof(T));

        public static ObjectConstructor CreateILConstructorDelegate(Type type)
        {
            ConstructorInfo emptyConstructor =
                type.GetConstructor(Type.EmptyTypes)
                ?? throw new NotSupportedException($"No parameterless constructor found for type {type.FullName}");

            DynamicMethod dynamicMethod = new("CreateInstance", type, Type.EmptyTypes, type, true);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Newobj, emptyConstructor);
            ilGenerator.Emit(OpCodes.Ret);
            return (ObjectConstructor)dynamicMethod.CreateDelegate(typeof(ObjectConstructor));
        }

        public static T CreateInstanceExpression<T>() =>
            (T)CreateExpressionConstructorDelegate<T>()();

        public static object CreateInstanceExpression(Type type) =>
            CreateExpressionConstructorDelegate(type)();

        public static Func<object> CreateExpressionConstructorDelegate<T>() =>
            CreateExpressionConstructorDelegate(typeof(T));

        public static Func<object> CreateExpressionConstructorDelegate(Type type)
        {
            ConstructorInfo emptyConstructor =
                type.GetConstructor(Type.EmptyTypes)
                ?? throw new NotSupportedException($"No parameterless constructor found for type {type.FullName}");

            NewExpression newExpr = Expression.New(emptyConstructor);
            Expression<Func<object>> lambdaExpr = Expression.Lambda<Func<object>>(newExpr);
            Func<object> func = lambdaExpr.Compile();
            return func;
        }

        public static object? CreateInstance(Type type, object[] args)
        {
            DynamicMethod dynamicCtor = CreateDynamicConstructor(type);
            return dynamicCtor.Invoke(null, args);
        }

        public static DynamicMethod CreateDynamicConstructor(Type type)
        {
            ConstructorInfo constructor =
                type
                    .GetConstructors()
                    .First();

            ParameterInfo[] parameters = constructor.GetParameters();
            Type[] argTypes = parameters.Select(x => x.ParameterType).ToArray();

            DynamicMethod dynamicMethod = new($"{type.FullName}_Create", type, argTypes, typeof(ObjectActivator), true);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            for (int i = 0; i < parameters.Length; ++i)
            {
                var param = parameters[i];
                
                if(param.ParameterType.IsNullableType())
                {
                    ilGenerator.Emit(OpCodes.Ldarga_S, i);
                    var underlyingType = Nullable.GetUnderlyingType(param.ParameterType);
                    var getValue = param.ParameterType.GetProperty("Value").GetGetMethod();
                    ilGenerator.Emit(OpCodes.Call, getValue);
                    ilGenerator.Emit(OpCodes.Newobj, param.ParameterType.GetConstructors().First());
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarg_S, i);
                }
            }

            ilGenerator.Emit(OpCodes.Newobj, constructor);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }
    }
}
