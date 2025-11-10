using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Threading.Caching
{
    record Result<TValue>(TValue Value, Exception? Error);
    record Message<TKey, TValue>(TKey Key, Func<CancellationToken, ValueTask<TValue>> Factory, TaskCompletionSource<TValue> Sender);

    public class CacheFactoryException(string? message, Exception? innerException) : Exception(message, innerException)
    {
    }

    public record MemoryCacheOptions
    (
        int Capacity = 0,
        TimeSpan? TimeToLive = null,
        TimeSpan? EvictionInterval = null,
        bool ThrowIfCapacityExceeded = false,
        bool SlidingExpiration = false
    );

    public sealed class MemoryCache<TKey, TValue> : IDisposable
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, CacheChannel<TKey, TValue>> _channelPool = [];
        private readonly ConcurrentDictionary<TKey, ICacheItem<TValue>> _cache = [];
        private readonly ConcurrentQueue<TKey> _addedItemsQueue = [];
        private readonly ConcurrentQueue<TKey> _accessedItemsQueue = new(); // Tracks accessed keys that might expire
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AsyncLock _evictionLock = new();
#if NET5_0_OR_GREATER
#else
        private readonly AsyncLock _clearLock = new();
#endif
        private readonly Task _backgroundTask;

        public readonly int Capacity;
        public readonly TimeSpan TimeToLive;
        public readonly TimeSpan EvictionInterval;
        public readonly bool ThrowIfCapacityExceeded;
        public readonly bool SlidingExpiration;

        public int Count => _cache.Count;
        public TKey[] Keys => _cache.Keys.ToArray();
        public TValue[] Values =>
            _cache
                .Values
                .Select(x => x.Value)
                .ToArray();

        public MemoryCache() : this(new MemoryCacheOptions())
        {
        }

        public MemoryCache(MemoryCacheOptions options)
        {
            _cancellationTokenSource = new();

            Capacity = options.Capacity;
            TimeToLive = options.TimeToLive ?? TimeSpan.FromMinutes(5);
            EvictionInterval = options.EvictionInterval ?? new TimeSpan(Math.Max(new TimeSpan(TimeToLive.Ticks / 100L * 10).Ticks, TimeSpan.FromMilliseconds(300).Ticks)); // 10%

            _backgroundTask = DoBackgroundWorkAsync();
            ThrowIfCapacityExceeded = options.ThrowIfCapacityExceeded;
            SlidingExpiration = options.SlidingExpiration;
        }

        public async ValueTask<TValue> GetOrCreateAsync(TKey key, Func<CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default)
        {
            if (TryGetValue(key, out TValue? cacheItemValue))
            {
                return cacheItemValue!;
            }

            if (ThrowIfCapacityExceeded && _cache.Count - Capacity == 0)
            {
                throw new IndexOutOfRangeException("The capacity has been exceeded");
            }

            TaskCompletionSource<TValue> self = new();
            CacheChannel<TKey, TValue> channel = GetCacheChannel(key);
            channel.Start();

            Message<TKey, TValue> msg = new(key, valueFactory, self);
            await channel.WriteAsync(msg, cancellationToken).ConfigureAwait(false);

            try
            {
                return await self.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new CacheFactoryException("An error occurred in creating the value", ex);
            }
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
                    if (_cache.TryUpdate(key, cacheItem.WithUpdatedAccessTime(), cacheItem))
                    {
                        _accessedItemsQueue.Enqueue(key); // Re-enqueue the key when accessed
                        value = cacheItem.Value;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryRemove(TKey key, out TValue? value)
        {
            bool removed = _cache.TryRemove(key, out ICacheItem<TValue>? cacheItem);
            value = removed ? cacheItem!.Value : default;
            return removed;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task ClearAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            await CleanChannelPoolAsync().ConfigureAwait(false);
            _cache.Clear();

#if NET5_0_OR_GREATER
            _accessedItemsQueue.Clear();
            _addedItemsQueue.Clear();
#else
            using (await _clearLock.LockAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                ClearQueueNetStandard(_accessedItemsQueue);
                ClearQueueNetStandard(_addedItemsQueue);
            }
#endif
        }

        private static void ClearQueueNetStandard(ConcurrentQueue<TKey> queue)
        {
            while (queue.TryPeek(out _))
            {
                queue.TryDequeue(out _);
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (ThrowIfCapacityExceeded && _cache.Count - Capacity == 0)
            {
                throw new IndexOutOfRangeException("The capacity has been exceeded");
            }

            if (Capacity > 0)
            {
                _addedItemsQueue.Enqueue(key);
            }

            return _cache.TryAdd(key, CacheItem.Create(value, TimeToLive, SlidingExpiration));
        }

        public bool TryUpdate(TKey key, TValue value)
        {
            if (_cache.TryGetValue(key, out ICacheItem<TValue>? cacheItem))
            {
                if (cacheItem.IsExpired())
                {
                    _cache.TryRemove(key, out _);
                }
                else
                {
                    if (_cache.TryUpdate(key, CacheItem.FromExisting(cacheItem, value, TimeToLive, SlidingExpiration), cacheItem))
                    {
                        _accessedItemsQueue.Enqueue(key); // Re-enqueue the key when accessed
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsKey(TKey key) => _cache.ContainsKey(key);

        private CacheChannel<TKey, TValue> GetCacheChannel(TKey key) =>
            _channelPool
                .GetOrAdd(key,
                _ =>
                    new
                    (
                        async msg =>
                        {
                            if (!TryGetValue(msg.Key, out TValue? cacheItemValue))
                            {
                                // Check and evict before adding new item
                                ValueTask<TValue> factoryTask = msg.Factory(_cancellationTokenSource.Token);

                                await TryEvictOldestIfNeededAsync().ConfigureAwait(false);

                                cacheItemValue = await factoryTask.ConfigureAwait(false);

                                _cache.TryAdd(msg.Key, CacheItem.Create(cacheItemValue, TimeToLive, SlidingExpiration));

                                if (Capacity > 0)
                                {
                                    _addedItemsQueue.Enqueue(msg.Key);
                                }
                            }

                            return cacheItemValue!;
                        },
                        _cancellationTokenSource.Token
                    )
                );

        private Task DoBackgroundWorkAsync()
        {
            Task evictAccessedItemsTask = EvictExpiredAccessedItemsAsync();
            Task evictStaleItemsTask = EvictExpiredStaleItemsAsync();
            Task cleaningTask = LoopCleanChannelPoolAsync();

            return Task.WhenAll(evictAccessedItemsTask, evictStaleItemsTask, cleaningTask);
        }

        private async Task EvictExpiredAccessedItemsAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested && _accessedItemsQueue.TryDequeue(out TKey? key))
            {
                await Task.Delay(EvictionInterval, _cancellationTokenSource.Token).ConfigureAwait(false);
                TryEvictItemIfExpired(key);
            }
        }

        private async Task EvictExpiredStaleItemsAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(EvictionInterval, _cancellationTokenSource.Token).ConfigureAwait(false);

                // Find keys in the cache that are not currently in the cleanup queue
                foreach (TKey? key in _cache.Keys.Except(_accessedItemsQueue))
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    TryEvictItemIfExpired(key);
                }
            }
        }

        private void TryEvictItemIfExpired(TKey key)
        {
            if (_cache.TryGetValue(key, out ICacheItem<TValue>? cacheItem))
            {
                if (cacheItem.IsExpired())
                {
                    _cache.TryRemove(key, out _); // Expired, so remove
                }
                else
                {
                    _accessedItemsQueue.Enqueue(key); // Not expired, enqueue for future cleanup
                }
            }
        }

        private async Task LoopCleanChannelPoolAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(EvictionInterval, _cancellationTokenSource.Token).ConfigureAwait(false);
                await CleanChannelPoolAsync().ConfigureAwait(false);
            }
        }

        private async Task CleanChannelPoolAsync()
        {
            foreach (TKey key in _channelPool.Keys)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                if (_channelPool.TryRemove(key, out CacheChannel<TKey, TValue>? channel))
                {
                    try
                    {
                        channel.Complete();
                        await channel.StopAsync().ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore cancellation exceptions during cleanup
                    }
                }
            }
        }

        private async ValueTask TryEvictOldestIfNeededAsync()
        {
            if (Capacity <= 0
                || ThrowIfCapacityExceeded
                || _cache.Count - Capacity < 0)
            {
                return;
            }

            using (await _evictionLock.LockAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
            {
                // Re-check after acquiring lock
                if (_cache.Count - Capacity < 0)
                {
                    return;
                }

                int removedKeysCount = 0, itemsToRemoveCount = _cache.Count - Capacity + 1;

                while (removedKeysCount++ < itemsToRemoveCount && _addedItemsQueue.TryDequeue(out TKey? key))
                {
                    if (!_cache.ContainsKey(key))
                    {
                        removedKeysCount--;
                        continue;
                    }

                    _cache.TryRemove(key, out _);
                }
            }
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

            StopTask(_backgroundTask);

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
    }
}
