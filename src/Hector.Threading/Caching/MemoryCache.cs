using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Hector.Threading.Caching
{
    record Result<TValue>(TValue Value, Exception? Error);
    record Message<TKey, TValue>(TKey Key, Func<CancellationToken, ValueTask<TValue>> Factory, TaskCompletionSource<Result<TValue>> Sender);

    public class CacheFactoryException(string? message, Exception? innerException) : Exception(message, innerException)
    {
    }

    public sealed class MemCache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly Channel<Message<TKey, TValue>> _channel;
        private readonly ConcurrentDictionary<TKey, ICacheItem<TValue>> _cache = [];
        private readonly Lazy<ValueTask> _consumer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly TimeSpan _evictionInterval;
        private readonly Task _evictionTask;
        private readonly bool _throwIfCapacityExceeded;

        public readonly int Capacity;
        public readonly int MaxPoolSize;
        public readonly TimeSpan TimeToLive;

        public int Count => _cache.Count;

        public MemCache(int capacity = 0, int maxPoolSize = 100, TimeSpan? timeToLive = null, bool throwIfCapacityExceeded = false) // Default to 0 for unbounded channels
        {
            if (capacity <= 0)
            {
                // Unbounded channel (no backpressure, can grow indefinitely)
                _channel = Channel.CreateUnbounded<Message<TKey, TValue>>(
                    new UnboundedChannelOptions
                    {
                        AllowSynchronousContinuations = true,
                        SingleReader = true,
                        SingleWriter = false
                    });
            }
            else
            {
                // Bounded channel (with backpressure strategy: Wait Until Space Available)
                _channel = Channel.CreateBounded<Message<TKey, TValue>>(
                    new BoundedChannelOptions(capacity)
                    {
                        FullMode = BoundedChannelFullMode.Wait, // Wait for space to become available
                        SingleReader = true,
                        SingleWriter = false,
                        AllowSynchronousContinuations = true
                    });
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _consumer = new Lazy<ValueTask>(() => ConsumeAsync(_cancellationTokenSource.Token));

            Capacity = capacity;
            MaxPoolSize = maxPoolSize;
            TimeToLive = timeToLive ?? TimeSpan.FromMinutes(5);

            _evictionInterval = new TimeSpan(TimeToLive.Ticks / 100L * 10); //10 %
            _evictionTask = Task.Run(() => EvictExpiredItemsAsync(_cancellationTokenSource.Token));
            _throwIfCapacityExceeded = throwIfCapacityExceeded;
        }

        public async ValueTask<TValue> GetOrCreateAsync(TKey key, Func<CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default)
        {
            if (TryGetValue(key, out TValue? cacheItemValue))
            {
                return cacheItemValue!;
            }

            TaskCompletionSource<Result<TValue>> self = new();

            Message<TKey, TValue> msg = new(key, valueFactory, self);
            await _channel.Writer.WriteAsync(msg, cancellationToken).ConfigureAwait(false);

            if (!_consumer.IsValueCreated)
            {
                _ = _consumer.Value;
            }

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

        private async ValueTask ConsumeAsync(CancellationToken cancellationToken)
        {
            bool isBounded = _channel.Reader.GetType().Name == "BoundedChannelReader";

            while (true)
            {
                if (isBounded)
                {
                    // For bounded channel, wait for a read without passing the cancellation token (for backpressure management).
                    if (!await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
                    {
                        break;
                    }
                }
                else
                {
                    // For unbounded channel, pass cancellation token to respect graceful cancellation.
                    if (!await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        break;
                    }
                }

                while (_channel.Reader.TryRead(out Message<TKey, TValue>? msg))
                {
                    TValue? value = default;
                    Exception? error = null;
                    try
                    {
                        if (!TryGetValue(msg.Key, out TValue? cacheItemValue))
                        {
                            // Check and evict before adding new item
                            TryEvictOldestIfNeeded();

                            value = await msg.Factory(cancellationToken).ConfigureAwait(false);
                            _cache.TryAdd(msg.Key, CacheItem.Create(value, TimeToLive));
                        }
                        else
                        {
                            value = cacheItemValue;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        msg.Sender.SetResult(new Result<TValue>(value!, error));
                    }
                }
            }
        }

        private async Task EvictExpiredItemsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_evictionInterval, cancellationToken).ConfigureAwait(false);

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

        private void TryEvictOldestIfNeeded()
        {
            if (Capacity <= 0 || _cache.Count < Capacity)
            {
                return;
            }

            if (_throwIfCapacityExceeded)
            {
                throw new IndexOutOfRangeException("The capacity has been exceeded");
            }

            // Find the oldest item
            KeyValuePair<TKey, ICacheItem<TValue>>? oldest = null;
            foreach (var item in _cache)
            {
                if (oldest == null || item.Value.LastAccess < oldest.Value.Value.LastAccess)
                {
                    oldest = item;
                }
            }

            // Remove the oldest item if found
            if (oldest.HasValue)
            {
                _cache.TryRemove(oldest.Value.Key, out _);
            }
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _cache.Clear();
            _cancellationTokenSource.Cancel();

            try
            {
                _consumer.Value.GetAwaiter().GetResult();
                _evictionTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }

            _cancellationTokenSource.Dispose();
        }
    }
}
