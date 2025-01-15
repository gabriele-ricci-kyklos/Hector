using Hector.Caching;
using System.Diagnostics;

namespace Hector.Tests
{
    public class SandboxTests
    {
        [Fact]
        public async Task TestCaching()
        {
            using MemCache<int, string> cache = new(100000);
            Stopwatch sw = Stopwatch.StartNew();

            foreach (int i in Enumerable.Range(0, 100000))
            {
                _ = cache.GetOrCreateAsync(i, (c) => { return ValueTask.FromResult("lol"); });
            }

            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;

            await cache.GetOrCreateAsync(2, (c) => ValueTask.FromResult("asd"));
            await cache.GetOrCreateAsync(2, (c) => ValueTask.FromResult("asd"));
        }
    }
}
