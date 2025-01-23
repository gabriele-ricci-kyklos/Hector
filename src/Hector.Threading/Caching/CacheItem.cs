using System;

namespace Hector.Threading.Caching
{
    interface ICacheItem<T>
    {
        T Value { get; }
        DateTime CreationTimestamp { get; }
        DateTime LastAccessTimestamp { get; }
        TimeSpan TimeToLive { get; }
        bool SlidingExpiration { get; }
        bool IsExpired();
        ICacheItem<T> WithUpdatedAccessTime();
    }

    static class CacheItem
    {
        internal static ICacheItem<T> Create<T>(T value, TimeSpan timeToLive, bool slidingExpiration)
        {
            Type type = typeof(T);

            bool shouldUseValueType =
                type.IsValueType
                || type.IsPrimitive
                || type == typeof(string);

            if (shouldUseValueType)
            {
                return new ValueCacheItem<T>(value, timeToLive, DateTime.UtcNow, slidingExpiration);
            }

            return new ReferenceCacheItem<T>(value, timeToLive, slidingExpiration);
        }
    }

    readonly struct ValueCacheItem<T>(T value, TimeSpan timeToLive, DateTime creationTimestamp, bool slidingExpiration) : ICacheItem<T>
    {
        public readonly DateTime CreationTimestamp { get; } = creationTimestamp;
        public readonly DateTime LastAccessTimestamp { get; } = DateTime.UtcNow;
        public readonly TimeSpan TimeToLive { get; } = timeToLive;
        public readonly bool SlidingExpiration { get; } = slidingExpiration;

        public T Value { get; } = value;

        public readonly ICacheItem<T> WithUpdatedAccessTime() =>
            new ValueCacheItem<T>(Value, TimeToLive, CreationTimestamp, SlidingExpiration);

        public readonly bool IsExpired() => DateTime.UtcNow - (SlidingExpiration ? LastAccessTimestamp : CreationTimestamp) > TimeToLive;
    }

    class ReferenceCacheItem<T>(T value, TimeSpan timeToLive, bool slidingExpiration) : ICacheItem<T>
    {
        public T Value { get; } = value;
        public DateTime CreationTimestamp { get; } = DateTime.UtcNow;
        public DateTime LastAccessTimestamp { get; private set; } = DateTime.UtcNow;
        public TimeSpan TimeToLive { get; } = timeToLive;
        public bool SlidingExpiration { get; } = slidingExpiration;

        public bool IsExpired() => DateTime.UtcNow - (SlidingExpiration ? LastAccessTimestamp : CreationTimestamp) > TimeToLive;

        public ICacheItem<T> WithUpdatedAccessTime()
        {
            LastAccessTimestamp = DateTime.UtcNow;
            return this;
        }
    }
}
