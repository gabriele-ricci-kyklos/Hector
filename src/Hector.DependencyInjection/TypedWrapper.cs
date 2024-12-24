namespace Hector.Core.DependencyInjection
{
    public class TypedWrapper<T>(T value)
    {
        public T Value { get; } = value;
    }

    public class TypedWrapper<T1, T2>(T1 value) : TypedWrapper<T1>(value)
    {
    }

    public class TypedWrapper<T1, T2, T3>(T1 value) : TypedWrapper<T1>(value)
    {
    }

    public class TypedWrapper<T1, T2, T3, T4>(T1 value) : TypedWrapper<T1>(value)
    {
    }
}
