using FluentAssertions;
using Xunit;

namespace Hector.Tests.NetFramework.Core.ExtensionMethods
{
    public class StringsExtensionMethodsTests
    {
        [Fact]
        public void ContainsIgnoreCase()
        {
            const string s = "hector is cool";
            s.ContainsIgnoreCase("HECTOR").Should().BeTrue();
        }
    }
}
