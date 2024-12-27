using FluentAssertions;
using Hector.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hector.Tests.NetFramework.Core.Collections
{
    public class CollectionTests
    {
        [Fact]
        public void TestBictionary()
        {
            Dictionary<int, string> dic = new Dictionary<int, string>()
            {
                { 1, "one" },
                { 2, "two" },
                { 3, "three" }
            };

            Bictionary<int, string> bictionary = new Bictionary<int, string>(dic);

            bictionary.GetForwardValueOrDefault(1).Should().Be("one");
            bictionary.GetReverseValueOrDefault("one").Should().Be(1);
        }
    }
}
