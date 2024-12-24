using FluentAssertions;
using Hector.Parallelism;

namespace Hector.Tests.Core.Parallelism
{
    public class ParallelismTests
    {
        [Fact]
        public async Task TestParallelLockManager()
        {
            Dictionary<string, int> data = new() { { "one", 0 }, { "two", 0 } };
            using ParallelLockManager<string> mutex = new();

            List<Task> tasks = [];
            foreach (int i in Enumerable.Range(1, 1000))
            {
                bool isEven = i % 2 == 0;
                string key = isEven ? "one" : "two";
                Task<int> task = mutex.ExecuteLockedCallAsync(key, () => Task.FromResult(data[key]++));
                //Task<int> task = Task.Run(() => Task.FromResult(data[key]++));
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            data["one"].Should().Be(500);
            data["two"].Should().Be(500);
        }
    }
}
