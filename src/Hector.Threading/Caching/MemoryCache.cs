using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Threading.Caching
{
    record Result<TValue>(TValue Value, Exception? Error);
    record Message<TKey, TValue>(TKey Key, Func<CancellationToken, ValueTask<TValue>> Factory, TaskCompletionSource<Result<TValue>> Sender);

    public class CacheFactoryException(string? message, Exception? innerException) : Exception(message, innerException)
    {
    }

    public sealed class MemCache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheChannel<TKey, TValue>> _channelPool = [];
        private readonly ConcurrentDictionary<TKey, ICacheItem<TValue>> _cache = [];
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _evictionTask;
        private readonly Task _channelPoolCleanupTask;
        private readonly bool _throwIfCapacityExceeded;

        public readonly int Capacity;
        public readonly int MaxPoolSize;
        public readonly TimeSpan TimeToLive;
        public readonly TimeSpan EvictionInterval;

        public int Count => _cache.Count;

        public MemCache(int capacity = 0, int maxPoolSize = 100, TimeSpan? timeToLive = null, TimeSpan? evictionInterval = null, bool throwIfCapacityExceeded = false) // Default to 0 for unbounded channels
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Capacity = capacity;
            MaxPoolSize = maxPoolSize;
            TimeToLive = timeToLive ?? TimeSpan.FromMinutes(5);
            EvictionInterval = evictionInterval ?? new TimeSpan(TimeToLive.Ticks / 100L * 10); //10 %

            _evictionTask = Task.Run(() => EvictExpiredItemsAsync(_cancellationTokenSource.Token));
            _channelPoolCleanupTask = Task.Run(() => CleanChannelPoolAsync(_cancellationTokenSource.Token));
            _throwIfCapacityExceeded = throwIfCapacityExceeded;
        }

        public async Task<TValue> GetOrCreateAsync(TKey key, Func<CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default)
        {
            if (TryGetValue(key, out TValue? cacheItemValue))
            {
                return cacheItemValue!;
            }

            TaskCompletionSource<Result<TValue>> self = new();
            CacheChannel<TKey, TValue> channel = GetCacheChannel(key);
            channel.Start();

            Message<TKey, TValue> msg = new(key, valueFactory, self);
            await channel.WriteAsync(msg, cancellationToken).ConfigureAwait(false);

            Result<TValue> result = await self.Task.ConfigureAwait(false);
            if (result.Error is not null)
            {
                throw new CacheFactoryException("An error occurred in creating the value", result.Error);
            }

            return result.Value;
        }

        public bool TryGetValue(TKey key, out TValue? value)
        {
            value = default;
            if (_cache.TryGetValue(key, out ICacheItem<TValue>? cacheItem))
            {
                if (cacheItem.IsExpired())
                {
                    _cache.TryRemove(key, out _);
                }
                else
                {
                    _cache.TryUpdate(key, cacheItem.WithUpdatedAccessTime(), cacheItem);
                    value = cacheItem.Value;
                    return true;
                }
            }

            return false;
        }

        public bool TryRemove(TKey key) => _cache.TryRemove(key, out _);
        public void Clear() => _cache.Clear();

        private CacheChannel<TKey, TValue> GetCacheChannel(TKey key) =>
            _channelPool
                .GetOrAdd(key,
                _ =>
                    new
                    (
                        async msg =>
                        {
                            TValue? value;
                            if (!TryGetValue(msg.Key, out TValue? cacheItemValue))
                            {
                                // Check and evict before adding new item
                                ValueTask<TValue> factoryTask = msg.Factory(_cancellationTokenSource.Token);

                                TryEvictOldestIfNeeded();

                                value = await factoryTask.ConfigureAwait(false);

                                _cache.TryAdd(msg.Key, CacheItem.Create(value, TimeToLive));
                            }
                            else
                            {
                                value = cacheItemValue;
                            }

                            return value;
                        },
                        _cancellationTokenSource.Token
                    )
                );

        private async Task EvictExpiredItemsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(EvictionInterval, cancellationToken).ConfigureAwait(false);

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.IsExpired())
                    {
                        // Directly remove the item if it's expired
                        _cache.TryRemove(kvp.Key, out _);
                    }
                }
            }
        }

        private async Task CleanChannelPoolAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(EvictionInterval, cancellationToken).ConfigureAwait(false);

                foreach (CacheChannel<TKey, TValue> channel in _channelPool.Values)
                {
                    try
                    {
                        channel.Complete();
                        channel.Stop();
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore cancellation exceptions during cleanup
                    }
                }
            }
        }

        private void TryEvictOldestIfNeeded()
        {
            if (Capacity <= 0 || _cache.Count - Capacity < 0)
            {
                return;
            }

            if (_throwIfCapacityExceeded)
            {
                throw new IndexOutOfRangeException("The capacity has been exceeded");
            }

            // Use a SortedSet to maintain a collection of the x smallest dates (oldest)
            SortedSet<(TKey Key, DateTime LastAccess)> oldestDates = new(new KeyLastAccessComparer());

            foreach (var item in _cache)
            {
                // If the set contains less than 'x' elements, just add the new date
                if (oldestDates.Count < (_cache.Count - Capacity + 1))
                {
                    oldestDates.Add((item.Key, item.Value.LastAccess));
                }
                else
                {
                    // If the set contains 'x' elements, only add if the current date is earlier
                    if (item.Value.LastAccess < oldestDates.Max.LastAccess)
                    {
                        oldestDates.Remove(oldestDates.Max); // Remove the largest (max) element
                        oldestDates.Add((item.Key, item.Value.LastAccess)); // Add the new smaller date
                    }
                }
            }

            foreach ((TKey key, _) in oldestDates)
            {
                _cache.TryRemove(key, out _);
            }

            oldestDates.Clear();
        }

        public void Dispose()
        {
            foreach (CacheChannel<TKey, TValue> item in _channelPool.Values)
            {
                item.Complete();
            }

            _cancellationTokenSource.Cancel();

            foreach (CacheChannel<TKey, TValue> item in _channelPool.Values)
            {
                try
                {
                    item.Stop();
                }
                catch (TaskCanceledException)
                {
                    // Ignore cancellation exceptions during cleanup
                }
            }

            _channelPool.Clear();
            _cache.Clear();

            StopTask(_evictionTask);
            StopTask(_channelPoolCleanupTask);

            _cancellationTokenSource.Dispose();
        }

        private static void StopTask(Task task)
        {
            try
            {
                task.GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation exceptions during cleanup
            }
        }

        private class KeyLastAccessComparer : IComparer<(TKey, DateTime)>
        {
            public int Compare((TKey, DateTime) x, (TKey, DateTime) y) => x.Item2.CompareTo(y.Item2);
        }
    }
}
