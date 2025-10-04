using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hector
{
    public static class TasksExtensionMethods
    {
        public static Task<T> AsResultTask<T>(this T result) => Task.FromResult(result);

        public static async Task<IEnumerable<T>> WhenAllAsync<T>(this IEnumerable<Task<T>> tasks) =>
            await Task
                .WhenAll(tasks.ToEmptyIfNull())
                .ConfigureAwait(false);

        public static ValueTask<T> AsResultValueTask<T>(this T result) => new(result);

        public static ValueTask<T> AsValueTask<T>(this Task<T> result) => new(result);
    }
}
