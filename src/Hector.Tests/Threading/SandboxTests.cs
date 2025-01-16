using Hector.Threading.Caching;
using System.Diagnostics;

namespace Hector.Tests
{
    public class ThreadingTests
    {
        [Fact]
        public async Task TestCaching()
        {
            using MemCache<int, string> cache = new();
            long startTime = Stopwatch.GetTimestamp();

            List<Task> tasks = [];
            foreach (int i in Enumerable.Range(0, 100000))
            {
                tasks.Add(cache.GetOrCreateAsync(i, (c) => { return ValueTask.FromResult("lol"); }));
            }

            await Task.WhenAll(tasks);

            TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTime);

            //Stopwatch sw = Stopwatch.StartNew();

            //var t1 = cache.GetOrCreateAsync(1, async (c) => { await Task.Delay(5000, c); return "lol"; });
            //var t2 = cache.GetOrCreateAsync(2, async (c) => { await Task.Delay(5000, c); return "asd"; });

            //await Task.WhenAll(t1.AsTask(), t2.AsTask());

            //var elapsed = sw.ElapsedMilliseconds;

            bool x = true;
        }
    }
}
