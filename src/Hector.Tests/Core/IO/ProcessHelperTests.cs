using FluentAssertions;
using Hector.IO;

namespace Hector.Tests.Core.IO
{
    public class ProcessHelperTests
    {
        [Fact]
        public async Task TestProcessHelper()
        {
            (string output, string error) = await ProcessHelper.RunAsync("cmd.exe", "/c echo lol & exit");

            output.Contains("lol").Should().BeTrue();
            error.Should().BeNullOrWhiteSpace();
        }

        [Fact]
        public void TestDetectRunningProcess()
        {
            ProcessHelper.DetectRunningProcess("system").Should().BeTrue();
        }
    }
}
