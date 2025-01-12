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
                _ = cache.GetOrCreateAsync(i, (c) => { Task.Delay(500); return ValueTask.FromResult("lol"); });
            }

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;

            await cache.GetOrCreateAsync(2, (c) => ValueTask.FromResult("asd"));
            await cache.GetOrCreateAsync(2, (c) => ValueTask.FromResult("asd"));
        }
    }

    record struct Message<TKey, TValue>(TKey Key, Func<CancellationToken, ValueTask<TValue>> Factory, Channel<TValue> Sender);

    public sealed class Cache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly Channel<Message<TKey, TValue>> _channel;
        private readonly ConcurrentDictionary<TKey, TValue> _cache = [];
        private readonly Lazy<ValueTask> _consumer;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Cache()
        {
            _channel =
                Channel
                    .CreateUnbounded<Message<TKey, TValue>>
                    (
                        new UnboundedChannelOptions
                        {
                            AllowSynchronousContinuations = true,
                            SingleReader = true,
                            SingleWriter = false
                        }
                    );

            _cancellationTokenSource = new();

            _consumer = new Lazy<ValueTask>(() => ConsumeAsync(_cancellationTokenSource.Token));
        }

        public async ValueTask<TValue> GetOrCreateAsync(TKey key, Func<CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out TValue? value))
            {
                return value;
            }

            Channel<TValue> self =
                Channel
                    .CreateBounded<TValue>
                    (
                        new BoundedChannelOptions(1)
                        {
                            SingleReader = true,
                            SingleWriter = true,
                            AllowSynchronousContinuations = true,
                            FullMode = BoundedChannelFullMode.Wait
                        }
                    );

            Message<TKey, TValue> msg = new(key, valueFactory, self);
            await _channel.Writer.WriteAsync(msg, cancellationToken).ConfigureAwait(false);

            if (!_consumer.IsValueCreated)
            {
                _ = _consumer.Value;
            }

            await self.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
            if (!self.Reader.TryRead(out value))
            {
                throw new Exception($"Unable to create a value for the key {key}");
            }

            return value;
        }

        private async ValueTask ConsumeAsync(CancellationToken cancellationToken)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out Message<TKey, TValue> msg))
                {
                    if (!_cache.TryGetValue(msg.Key, out TValue? value))
                    {
                        value = await msg.Factory(cancellationToken).ConfigureAwait(false);
                        _cache.TryAdd(msg.Key, value);
                    }

                    await msg.Sender.Writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
                    msg.Sender.Writer.Complete();
                }
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
            }
            catch (OperationCanceledException)
            {
            }

            _cancellationTokenSource.Dispose();
        }
    }
}
