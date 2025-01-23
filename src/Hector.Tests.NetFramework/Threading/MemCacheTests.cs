using FluentAssertions;
using Hector.Threading.Caching;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hector.Tests.NetFramework.Threading
{
    public class MemCacheTests
    {
        [Fact]
        public async Task ClearAsync_ShouldRemoveAllItems()
        {
            using (MemCache<string, int> cache = new MemCache<string, int>())
            {
                // Arrange
                await cache.GetOrCreateAsync("key1", _ => new ValueTask<int>(1));
                await cache.GetOrCreateAsync("key2", _ => new ValueTask<int>(1));

                // Act
                await cache.ClearAsync();

                // Assert
                cache.Count.Should().Be(0);
            }
        }
    }
}
