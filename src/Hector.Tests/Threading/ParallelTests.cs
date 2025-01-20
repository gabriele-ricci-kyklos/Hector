using FluentAssertions;
using Hector.Threading.Parallel;

namespace Hector.Tests.Threading
{
    public class ParallelTests
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

        [Fact]
        public async Task TestDoInParallelColletingResultsAsync()
        {
            List<int> models = [1, 2, 3];

            Dictionary<int, int[]> results =
                await ParallelHelper
                    .DoInParallelColletingResultsAsync
                    (
                        models.ToArray(),
                        (a, b) =>
                        {
                            int[] r = [1, 2];
                            return ValueTask.FromResult(r);
                        }
                    );

            results.Should().NotBeNullOrEmpty()
                .And.HaveCount(3)
                .And.AllSatisfy(x => x.Value.Should().NotBeNullOrEmpty().And.HaveCount(2));
        }
    }
}
