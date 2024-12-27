using FluentAssertions;
using Hector.Parallelism;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hector.Tests.NetFramework.Core.Parallelism
{
    public class ParallelismTests
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
                        (a, b) =>
                        {
                            int[] r = new int[] { 1, 2 };
                            return Task.FromResult(r);
                        }
                    );

            results.Should().NotBeNullOrEmpty()
                .And.HaveCount(3)
                .And.AllSatisfy(x => x.Value.Should().NotBeNullOrEmpty().And.HaveCount(2));
        }
    }
}
