using Hector.Threading.Caching;
using System.Diagnostics;

namespace Hector.Tests
{
    public class ThreadingTests
    {
        [Fact]
        public async Task TestCaching()
        {
            using MemCache<int, string> cache = new(1);
            long startTime = Stopwatch.GetTimestamp();

            List<Task> tasks = [];
            foreach (int i in Enumerable.Range(0, 100000))
            {
                tasks.Add(cache.GetOrCreateAsync(i, (c) => { return ValueTask.FromResult("lol"); }));
            }

            await Task.WhenAll(tasks);

            TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);

            //await cache.GetOrCreateAsync(1, c => ValueTask.FromResult("lol"));
            //await cache.GetOrCreateAsync(2, c => ValueTask.FromResult("asd"));

            bool x = true;
        }
    }
}
