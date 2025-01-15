using System;

namespace Hector.Threading.Caching
{
    interface ICacheItem<T>
    {
        T Value { get; }
        DateTime LastAccess { get; }
        TimeSpan TimeToLive { get; }
        bool IsExpired();
        ICacheItem<T> WithUpdatedAccessTime();
    }

    static class CacheItem
    {
        internal static ICacheItem<T> Create<T>(T value, TimeSpan timeToLive)
        {
            Type type = typeof(T);

            bool shouldUseValueType =
                type.IsValueType
                || type.IsPrimitive
                || type == typeof(string);

            if (shouldUseValueType)
            {
                return new ValueCacheItem<T>(value, timeToLive);
            }
            return new ReferenceCacheItem<T>(value, timeToLive);
        }
    }

    readonly struct ValueCacheItem<T>(T value, TimeSpan timeToLive) : ICacheItem<T>
    {
        public readonly DateTime LastAccess { get; } = DateTime.UtcNow;
        public readonly TimeSpan TimeToLive { get; } = timeToLive;

        public T Value { get; } = value;

        public readonly ICacheItem<T> WithUpdatedAccessTime() => new ValueCacheItem<T>(Value, TimeToLive);

        public readonly bool IsExpired() => DateTime.UtcNow - LastAccess > TimeToLive;
    }

    class ReferenceCacheItem<T>(T value, TimeSpan timeToLive) : ICacheItem<T>
    {
        public T Value { get; } = value;
        public TimeSpan TimeToLive { get; } = timeToLive;
        public DateTime LastAccess { get; private set; } = DateTime.UtcNow;

        public bool IsExpired() => DateTime.UtcNow - LastAccess > TimeToLive;

        public ICacheItem<T> WithUpdatedAccessTime()
        {
            LastAccess = DateTime.UtcNow;
            return this;
        }
    }
}
