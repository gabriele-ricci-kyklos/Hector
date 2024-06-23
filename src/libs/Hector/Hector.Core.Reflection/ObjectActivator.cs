using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Hector.Core.Reflection
{
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
    }
}
