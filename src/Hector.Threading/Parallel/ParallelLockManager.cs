using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Threading.Parallel
{
    public class ParallelLockManager<TKey>(int initialCount = 1, int maxCount = 1) : IDisposable
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _keyLocks = new();

        public Task<T> ExecuteLockedCallAsync<T>(TKey key, Func<Task<T>> action, int timeoutSeconds, CancellationToken ctoken = default) =>
            ExecuteLockedCallAsync(key, action, timeoutSeconds.Seconds(), ctoken);

        public async Task<T> ExecuteLockedCallAsync<T>(TKey key, Func<Task<T>> action, TimeSpan? timeout = null, CancellationToken ctoken = default)
        {
            SemaphoreSlim lockItem = _keyLocks.GetOrAdd(key, x => new SemaphoreSlim(initialCount, maxCount));

            try
            {
                await lockItem.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), ctoken).ConfigureAwait(false);
                return await action().ConfigureAwait(false);
            }
            finally
            {
                lockItem.Release();
            }
        }

        public void Dispose()
        {
            foreach (SemaphoreSlim item in _keyLocks.Values)
            {
                item.Dispose();
            }
        }
    }
}
