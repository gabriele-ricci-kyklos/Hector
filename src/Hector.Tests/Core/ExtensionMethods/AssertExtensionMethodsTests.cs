using FluentAssertions;
using Hector;
using System;

namespace Hector.Tests.Core.ExtensionMethods
{
    public class AssertExtensionMethodsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        public void TestGetNonNullOrThrow(string? testValue)
        {
            Func<string?, string> func = x => x.GetNonNullOrThrow(nameof(x));

            if (testValue is null)
            {
                func
                    .Invoking(f => f(testValue))
                    .Should().Throw<ArgumentNullException>();
            }    
            else
            {
                func(testValue).Should().Be(testValue);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        public void TestGetNonNullOrThrowWithExFactory(string? testValue)
        {
            Func<string?, string> func = x => x.GetNonNullOrThrow(() => throw new NotSupportedException("custom msg"));

            if (testValue is null)
            {
                func
                    .Invoking(f => f(testValue))
                    .Should().Throw<NotSupportedException>();
            }
            else
            {
                testValue
                    .GetNonNullOrThrow(() => throw new NotSupportedException("custom msg"))
                    .Should().Be(testValue);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void TestGetValidatedOrThrow(int testValue)
        {
            Func<int, bool> func = x => x == 1;
            if (func(testValue))
            {
                int newValue = testValue.GetValidatedOrThrow(func, nameof(newValue));
                newValue.Should().Be(testValue);
            }
            else
            {
                testValue
                    .Invoking(x => x.GetValidatedOrThrow(func, nameof(x)))
                    .Should().Throw<ArgumentException>();
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        public void TestGetTextOrThrow(string? testValue)
        {
            Func<string?, string> func = x => x.GetTextOrThrow(nameof(x));

            if (testValue is null)
            {
                func.Invoking(f => f(testValue)).Should().Throw<ArgumentException>();
            }
            else
            {
                string newValue = func(testValue);
                newValue.Should().Be(testValue);
            }
        }
    }
}
