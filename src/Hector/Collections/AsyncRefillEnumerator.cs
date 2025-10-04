using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Collections
{
    public sealed class AsyncRefillEnumerator<T>(Func<int, Task<T[]>> factory, int windowSize)
    {
        private AsyncLazy<ConcurrentQueue<T>>? _lazyQueue;

        public Task<T> GetNextValueAsync() => InternalGetNextValueAsync();

        private async Task<T> InternalGetNextValueAsync(int depth = 0)
        {
            AsyncLazy<ConcurrentQueue<T>> lazyQueue =
                Interlocked.CompareExchange(ref _lazyQueue, new AsyncLazy<ConcurrentQueue<T>>(CreateQueueAsync), null)
                ?? _lazyQueue;

            ConcurrentQueue<T> queue = await lazyQueue.Value.ConfigureAwait(false);

            if (queue.TryDequeue(out T? value))
            {
                return value;
            }
            else if (depth > 1)
            {
                throw new InvalidOperationException("Unable to generate more values");
            }

            Interlocked.CompareExchange(ref _lazyQueue, null, lazyQueue);
            return await InternalGetNextValueAsync(++depth).ConfigureAwait(false);
        }

        private async Task<ConcurrentQueue<T>> CreateQueueAsync()
        {
            T[] items = await factory(windowSize).ConfigureAwait(false);
            return new ConcurrentQueue<T>(items);
        }
    }
}
