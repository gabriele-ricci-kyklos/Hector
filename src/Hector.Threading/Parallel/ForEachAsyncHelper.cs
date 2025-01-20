using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Threading.Parallel
{
    internal class ForEachAsyncHelper
    {
        internal static Task Parallel_ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
        {
#if NET5_0_OR_GREATER
            return System.Threading.Tasks.Parallel.ForEachAsync(source, parallelOptions, body);
#else
            return Parallel_ForEachAsync_NetStandard(source, parallelOptions, body);
#endif
        }

        //Credits: https://stackoverflow.com/a/65251949/4499267
        internal static Task Parallel_ForEachAsync_NetStandard<T>(IEnumerable<T> source,
    ParallelOptions parallelOptions,
    Func<T, CancellationToken, ValueTask> body)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (parallelOptions == null) throw new ArgumentNullException("parallelOptions");
            if (body == null) throw new ArgumentNullException("body");
            int dop = parallelOptions.MaxDegreeOfParallelism;
            if (dop < 0) dop = Environment.ProcessorCount;
            CancellationToken cancellationToken = parallelOptions.CancellationToken;
            TaskScheduler scheduler = parallelOptions.TaskScheduler ?? TaskScheduler.Current;

            IEnumerator<T> enumerator = source.GetEnumerator();
            CancellationTokenSource cts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken);
            SemaphoreSlim semaphore = new(1, 1); // Synchronizes the enumeration
            Task[] workerTasks = new Task[dop];
            for (int i = 0; i < dop; ++i)
            {
                workerTasks[i] = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            if (cts.IsCancellationRequested)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                break;
                            }
                            T item;
                            await semaphore.WaitAsync(); // Continue on captured context.
                            try
                            {
                                if (!enumerator.MoveNext()) break;
                                item = enumerator.Current;
                            }
                            finally { semaphore.Release(); }
                            await body(item, cts.Token); // Continue on captured context.
                        }
                    }
                    catch { cts.Cancel(); throw; }
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler)
                    .Unwrap();
            }
            return Task.WhenAll(workerTasks).ContinueWith(t =>
            {
                // Clean up (dispose all disposables)
                using (enumerator) using (cts) using (semaphore) { }
                return t;
            }, CancellationToken.None, TaskContinuationOptions.DenyChildAttach |
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
        }
    }
}
