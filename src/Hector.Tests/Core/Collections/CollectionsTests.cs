using FluentAssertions;
using Hector.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hector.Tests.Core.Collections
{
    public class CollectionsTests
    {
        [Fact]
        public void TestBictionary()
        {
            Dictionary<int, string> dict = new()
            {
                { 1, "one" },
                { 2, "two" },
                { 3, "three" }
            };

            Bictionary<int, string> bictionary = new(dict);

            bictionary.GetForwardValueOrDefault(1).Should().Be("one");
            bictionary.GetReverseValueOrDefault("one").Should().Be(1);
        }

        [Fact]
        public void TestEnumeratorWrapper()
        {
            int[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            using EnumeratorWrapper<int> enumeratorWrapper = new(data);

            enumeratorWrapper.NextValue.Should().Be(data[0]);

            enumeratorWrapper.Current.Should().Be(data[0]);

            enumeratorWrapper.EnumerateRangeValues(3)
                .ToArray() //calculating the enumerable to prevent FluentAssertion's methods to move forward
                .Should().NotBeNullOrEmpty()
                .And.HaveCount(3)
                .And.BeInAscendingOrder();

            enumeratorWrapper.GetRangeValues(4)
                .Should().NotBeNullOrEmpty()
                .And.HaveCount(4)
                .And.BeInAscendingOrder();

            enumeratorWrapper.SafeNextValue.Should().Be(data[8]);

            enumeratorWrapper.SafeNextValue.Should().Be(0);
            enumeratorWrapper.Invoking(x => x.NextValue).Should().Throw<IndexOutOfRangeException>();
        }
    }
}
