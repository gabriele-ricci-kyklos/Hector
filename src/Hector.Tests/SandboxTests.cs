using Hector.Tests.Core.ExtensionMethods;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace Hector.Tests
{
    public class SandboxTests
    {
        [Fact]
        public async Task TestCaching()
        {
            using Cache<int, string> cache = new();
            Stopwatch sw = Stopwatch.StartNew();

            foreach (int i in Enumerable.Range(0, 100000))
            {
                _ = cache.GetOrCreateAsync(i, (c) => { return ValueTask.FromResult("lol"); });
            }

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;

            await cache.GetOrCreateAsync(2, (c) => ValueTask.FromResult("asd"));
            await cache.GetOrCreateAsync(2, (c) => ValueTask.FromResult("asd"));
        }
    }

    record Result<TValue>(TValue Value, Exception? Error);
    record Message<TKey, TValue>(TKey Key, Func<CancellationToken, ValueTask<TValue>> Factory, TaskCompletionSource<Result<TValue>> Sender);

    public class CacheFactoryException(string? message, Exception? innerException) : Exception(message, innerException)
    {
    }

    class CacheItem<T>
    {
        private readonly DateTime _creationDate;
        private readonly T _value;

        private DateTime _lastAccess;

        public T Value
        {
            get
            {
                _lastAccess = DateTime.Now.ToUniversalTime();
                return _value;
            }
        }

        public CacheItem(T value)
        {
            _value = value;
            _creationDate = DateTime.Now.ToUniversalTime();
            _lastAccess = _creationDate;
        }
    }

    public sealed class Cache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly Channel<Message<TKey, TValue>> _channel;
        private readonly ConcurrentDictionary<TKey, CacheItem<TValue>> _cache = [];
        private readonly Lazy<ValueTask> _consumer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentBag<Channel<Result<TValue>>> _channelPool = [];
        private readonly ConcurrentDictionary<TKey, TaskCompletionSource<Result<TValue>>> _pendingRequests = new();

        public int Capacity;
        public int MaxPoolSize;

        public Cache(int capacity = 0, int maxPoolSize = 100) // Default to 0 for unbounded channels
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
        }

        public async ValueTask<TValue> GetOrCreateAsync(TKey key, Func<CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out CacheItem<TValue>? cacheItem))
            {
                return cacheItem.Value;
            }

            // If not in cache, check if there's a pending request for the key
            TaskCompletionSource<Result<TValue>> self = _pendingRequests.GetOrAdd(key, _ => new());


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

        private async ValueTask ConsumeAsync(CancellationToken cancellationToken)
        {
            bool isUnbounded = _channel.Reader.GetType().Name == "UnboundedChannelReader";

            while (true)
            {
                if (!isUnbounded)
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
                        if (!_cache.TryGetValue(msg.Key, out CacheItem<TValue>? cacheItem))
                        {
                            value = await msg.Factory(cancellationToken).ConfigureAwait(false);
                            cacheItem = new CacheItem<TValue>(value);
                            _cache.TryAdd(msg.Key, cacheItem);
                        }
                        else
                        {
                            value = cacheItem.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        msg.Sender.SetResult(new Result<TValue>(value!, error));
                        _pendingRequests.TryRemove(msg.Key, out _);
                    }
                }
            }
        }

        // Factory method to create or get a channel from the pool
        private Channel<Result<TValue>> GetOrCreateInnerChannel()
        {
            // Try to take a channel from the pool
            if (_channelPool.TryTake(out Channel<Result<TValue>>? channel))
            {
                return channel;  // Return an available channel from the pool
            }

            if (_channelPool.Count < MaxPoolSize)  // If the pool size is less than the max allowed
            {
                // Create a new bounded channel and add it to the pool
                channel =
                    Channel
                        .CreateBounded<Result<TValue>>
                        (
                            new BoundedChannelOptions(1)
                            {
                                SingleReader = true,
                                SingleWriter = true,
                                AllowSynchronousContinuations = true,
                                FullMode = BoundedChannelFullMode.Wait
                            }
                        );

                return channel;
            }

            // If the pool is full, we throw an exception or apply other strategies.
            throw new InvalidOperationException("Channel pool is full, cannot create a new channel.");
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _cache.Clear();
            _cancellationTokenSource.Cancel();

            try
            {
                _consumer.Value.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }

            _cancellationTokenSource.Dispose();
        }
    }
}
