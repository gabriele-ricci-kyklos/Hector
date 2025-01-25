using FluentAssertions;
using Hector.Threading.Caching;

namespace Hector.Tests.Threading
{
    public class MemoryCacheTests
    {
        [Fact]
        public async Task GetOrCreateAsync_ShouldAddItemToCache_WhenNotAlreadyPresent()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 10);
            using MemoryCache<string, string> cache = new(options);

            // Act
            string value = await cache.GetOrCreateAsync("key1", _ => ValueTask.FromResult("value1"));

            // Assert
            value.Should().Be("value1");
            cache.TryGetValue("key1", out string? cachedValue).Should().BeTrue();
            cachedValue.Should().Be("value1");
        }

        [Fact]
        public async Task GetOrCreateAsync_ShouldReturnExistingValue_WhenKeyExists()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 10);
            using MemoryCache<string, string> cache = new(options);
            await cache.GetOrCreateAsync("key1", _ => ValueTask.FromResult("value1"));

            // Act
            string value = await cache.GetOrCreateAsync("key1", _ => ValueTask.FromResult("value2"));

            // Assert
            value.Should().Be("value1");
        }

        [Fact]
        public void TryGetValue_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 10);
            using MemoryCache<string, string> cache = new(options);

            // Act
            bool result = cache.TryGetValue("key1", out string? value);

            // Assert
            result.Should().BeFalse();
            value.Should().BeNull();
        }

        [Fact]
        public void TryAdd_ShouldAddItem_WhenKeyDoesNotExist()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 10);
            using MemoryCache<string, string> cache = new(options);

            // Act
            bool result = cache.TryAdd("key1", "value1");

            // Assert
            result.Should().BeTrue();
            cache.TryGetValue("key1", out string? value).Should().BeTrue();
            value.Should().Be("value1");
        }

        [Fact]
        public void TryAdd_ShouldThrowException_WhenCapacityExceededAndThrowIfCapacityExceededIsTrue()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 1, ThrowIfCapacityExceeded: true);
            using MemoryCache<string, string> cache = new(options);
            cache.TryAdd("key1", "value1");

            // Act
            Action act = () => cache.TryAdd("key2", "value2");

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }

        [Fact]
        public void TryRemove_ShouldRemoveItem_WhenKeyExists()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 10);
            using MemoryCache<string, string> cache = new(options);
            cache.TryAdd("key1", "value1");

            // Act
            bool result = cache.TryRemove("key1", out string? removedValue);

            // Assert
            result.Should().BeTrue();
            removedValue.Should().Be("value1");
            cache.TryGetValue("key1", out _).Should().BeFalse();
        }

        [Fact]
        public async Task Eviction_ShouldRemoveOldestItem_WhenCapacityExceeded()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 2);
            using MemoryCache<string, string> cache = new(options);
            await cache.GetOrCreateAsync("key1", _ => ValueTask.FromResult("value1"));
            await cache.GetOrCreateAsync("key2", _ => ValueTask.FromResult("value2"));

            // Act
            await cache.GetOrCreateAsync("key3", _ => ValueTask.FromResult("value3"));

            // Assert
            cache.TryGetValue("key1", out _).Should().BeFalse();
            cache.TryGetValue("key2", out _).Should().BeTrue();
            cache.TryGetValue("key3", out _).Should().BeTrue();
        }

        [Fact]
        public async Task Item_ShouldExpire_WhenTimeToLiveExceeded()
        {
            // Arrange
            MemoryCacheOptions options = new(TimeToLive: TimeSpan.FromMilliseconds(300));
            using MemoryCache<string, string> cache = new(options);
            await cache.GetOrCreateAsync("key1", _ => ValueTask.FromResult("value1"));

            // Act
            await Task.Delay(500);
            bool result = cache.TryGetValue("key1", out _);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SlidingExpiration_ShouldResetExpirationOnAccess()
        {
            int ttlMilliseconds = 300;
            int halfTTL = ttlMilliseconds / 2;

            // Arrange
            using MemoryCache<string, int> cache = new(new(
                TimeToLive: TimeSpan.FromMilliseconds(ttlMilliseconds),
                SlidingExpiration: true
            ));

            string key = "test";
            await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult(42));
            await Task.Delay(halfTTL); // Half of TTL

            // Act
            bool firstAccess = cache.TryGetValue(key, out int _);
            await Task.Delay(halfTTL); // Another half
            bool secondAccess = cache.TryGetValue(key, out int _);

            await Task.Delay(ttlMilliseconds); // Whole TTL

            bool thirdAccess = cache.TryGetValue(key, out int _);

            // Assert
            firstAccess.Should().BeTrue();
            secondAccess.Should().BeTrue();
            thirdAccess.Should().BeFalse();
        }

        [Fact]
        public async Task ClearAsync_ShouldRemoveAllItems()
        {
            using MemoryCache<string, int> cache = new();

            // Arrange
            await cache.GetOrCreateAsync("key1", _ => ValueTask.FromResult(1));
            await cache.GetOrCreateAsync("key2", _ => ValueTask.FromResult(1));

            // Act
            await cache.ClearAsync();

            // Assert
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void ContainsKey_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 10);
            using MemoryCache<string, string> cache = new(options);
            cache.TryAdd("key1", "value1");

            // Act
            bool result = cache.ContainsKey("key1");
            bool result2 = cache.ContainsKey("key2");

            // Assert
            result.Should().BeTrue();
            result2.Should().BeFalse();
        }

        [Fact]
        public void TryUpdate_ShouldUpdateValue_WhenKeyExistsAndNotExpired()
        {
            // Arrange
            MemoryCacheOptions options = new(Capacity: 10);
            using MemoryCache<string, string> cache = new(options);
            cache.TryAdd("key1", "value1");

            // Act
            bool result = cache.TryUpdate("key1", "updatedValue");
            bool result2 = cache.TryUpdate("key2", "updatedValue");

            // Assert
            result.Should().BeTrue();
            cache.TryGetValue("key1", out string? value).Should().BeTrue();
            value.Should().Be("updatedValue");
            result2.Should().BeFalse();
        }

        [Fact]
        public async Task TryUpdate_ShouldReturnFalse_WhenKeyIsExpired()
        {
            // Arrange
            MemoryCacheOptions options = new(TimeToLive: TimeSpan.FromMilliseconds(100));
            using MemoryCache<string, string> cache = new(options);
            await cache.GetOrCreateAsync("key1", _ => ValueTask.FromResult("value1"));
            await Task.Delay(200);

            // Act
            bool result = cache.TryUpdate("key1", "updatedValue");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Test_factory_delegate_called_only_once()
        {
            // Arrange
            using MemoryCache<int, int> cache = new();
            int counter = 0;

            //Act
            List<Task> tasks = [];
            for(int i = 0; i < 1000; ++i)
            {
                tasks.Add(cache.GetOrCreateAsync(1, c =>
                {
                    Interlocked.Increment(ref counter);
                    return ValueTask.FromResult(1);
                }).AsTask());
            }

            await Task.WhenAll(tasks);

            // Assert
            counter.Should().Be(1);
        }
    }
}
