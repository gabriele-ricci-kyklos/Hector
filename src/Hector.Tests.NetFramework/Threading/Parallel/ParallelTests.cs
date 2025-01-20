using FluentAssertions;
using Hector.Threading.Parallel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hector.Tests.NetFramework.Core.Parallelism
{
    public class ParallelTests
    {
        [Fact]
        public async Task TestDoInParallelColletingResultsAsync()
        {
            List<int> models = new List<int>() { 1, 2, 3 };

            Dictionary<int, int[]> results =
                await ParallelHelper
                    .DoInParallelColletingResultsAsync
                    (
                        models.ToArray(),
                        async (a, b) =>
                        {
                            int[] r = new int[] { 1, 2 };
                            await Task.Delay(10);
                            return r;
                        }
                    );

            results.Should().NotBeNullOrEmpty()
                .And.HaveCount(3)
                .And.AllSatisfy(x => x.Value.Should().NotBeNullOrEmpty().And.HaveCount(2));
        }
    }
}
