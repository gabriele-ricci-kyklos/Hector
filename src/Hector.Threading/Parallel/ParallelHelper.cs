using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Threading.Parallel
{
    public class ParallelHelper
    {
        public static Task<Dictionary<TKey, TResult[]>> DoInParallelColletingResultsAsync<TKey, TResult>(TKey[] models, Func<TKey, CancellationToken, ValueTask<TResult[]>> actionTask, int degreeOfParallelism = 5)
            where TKey : notnull
        {
            Func<TKey, ConcurrentDictionary<TKey, TResult[]>, CancellationToken, ValueTask> action =
                async (x, y, z) =>
                {
                    TResult[] res = await actionTask(x, z);
                    y.TryAdd(x, res);
                };

            return DoInParallelColletingResultsAsync(models, action, degreeOfParallelism);
        }

        public static async Task<Dictionary<TKey, TResult[]>> DoInParallelColletingResultsAsync<TKey, TResult>(TKey[] models, Func<TKey, ConcurrentDictionary<TKey, TResult[]>, CancellationToken, ValueTask> actionTask, int degreeOfParallelism = 5)
            where TKey : notnull
        {
            ConcurrentDictionary<TKey, TResult[]> resultDataDict = new(degreeOfParallelism, models.Length);

            await ForEachAsyncHelper
                .Parallel_ForEachAsync
                (
                    models,
                    new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                    (x, c) => actionTask(x, resultDataDict, c)
                )
                .ConfigureAwait(false);

            return resultDataDict.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
