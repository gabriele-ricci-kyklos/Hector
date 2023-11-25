namespace Hector.Core.DependencyInjection
{
    public class TypedWrapper<T>
    {
        public T Value { get; }

        public TypedWrapper(T value)
        {
            Value = value;
        }
    }

    public class TypedWrapper<T1, T2> : TypedWrapper<T1>
    {
        public TypedWrapper(T1 value) : base(value)
        {
        }
    }

    public class TypedWrapper<T1, T2, T3> : TypedWrapper<T1>
    {
        public TypedWrapper(T1 value) : base(value)
        {
        }
    }

    public class TypedWrapper<T1, T2, T3, T4> : TypedWrapper<T1>
    {
        public TypedWrapper(T1 value) : base(value)
        {
        }
    }
}
