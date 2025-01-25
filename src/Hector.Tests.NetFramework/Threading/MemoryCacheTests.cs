using FluentAssertions;
using Hector.Threading.Caching;
using System.Threading.Tasks;
using Xunit;

namespace Hector.Tests.NetFramework.Threading
{
    public class MemoryCacheTests
    {
        [Fact]
        public async Task ClearAsync_ShouldRemoveAllItems()
        {
            using (MemoryCache<string, int> cache = new MemoryCache<string, int>())
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
