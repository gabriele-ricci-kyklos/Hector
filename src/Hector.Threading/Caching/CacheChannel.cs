using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Hector.Threading.Caching
{
    internal sealed class CacheChannel<TKey, TValue> where TKey : notnull
    {
        private readonly Channel<Message<TKey, TValue>> _channel;
        private readonly Lazy<Task> _consumer;
        private readonly Func<Message<TKey, TValue>, ValueTask<TValue?>> _factory;
        private readonly CancellationToken _cancellationToken;

        internal CacheChannel(Func<Message<TKey, TValue>, ValueTask<TValue?>> factory, CancellationToken cancellationToken)
        {
            _factory = factory;
            _cancellationToken = cancellationToken;
            _consumer = new Lazy<Task>(() => ConsumeAsync(_cancellationToken), true);
            _channel = NewChannel();
        }

        public ValueTask WriteAsync(Message<TKey, TValue> item, CancellationToken cancellationToken) =>
            _channel.Writer.WriteAsync(item, cancellationToken);

        //Since this class is created using GetOrAdd method from ConcurrentDictionary
        //It might be created more than once for each key, because GetOrAdd is not thread safe
        //So the start method is separated to avoid starting ConsumeAsync and not being able to stop it
        public void Start() => _ = _consumer.Value;

        public void Complete() => _channel.Writer.Complete();

        public void Stop() => _consumer.Value.GetAwaiter().GetResult();

        private async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            bool isBounded = _channel.Reader.GetType().Name == "BoundedChannelReader";

            while (!cancellationToken.IsCancellationRequested)
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
                        value = await _factory(msg).ConfigureAwait(false);
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

        private static Channel<Message<TKey, TValue>> NewChannel() =>
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
    }
}
