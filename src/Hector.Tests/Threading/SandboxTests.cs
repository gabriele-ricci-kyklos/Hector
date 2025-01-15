using Hector.Threading.Caching;
using System.Diagnostics;

namespace Hector.Tests
{
    public class ThreadingTests
    {
        [Fact]
        public async Task TestCaching()
        {
            using MemCache<int, string> cache = new(10);
            Stopwatch sw = Stopwatch.StartNew();

            foreach (int i in Enumerable.Range(0, 10))
            {
                _ = cache.GetOrCreateAsync(i, (c) => { return ValueTask.FromResult("lol"); });
            }

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;

            await cache.GetOrCreateAsync(100001, (c) => ValueTask.FromResult("asd"));
            await cache.GetOrCreateAsync(100002, (c) => ValueTask.FromResult("asd"));
            bool x = true;
        }
    }
}
