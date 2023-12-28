using Hector.Core;

namespace Hector.Tests.Core.ExtensionMethods
{
    public class AssertExtensionMethodsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        public void TestGetNonNullOrThrow(string? testValue)
        {
            if(testValue is null)
            {
                var ex = Assert.Throws<ArgumentNullException>(() => testValue.GetNonNullOrThrow(nameof(testValue)));
                Assert.IsType<ArgumentNullException>(ex);
            }    
            else
            {
                string newValue = testValue.GetNonNullOrThrow(nameof(newValue));
                Assert.Equal(testValue, newValue);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        public void TestGetNonNullOrThrowWithExFactory(string? testValue)
        {
            if (testValue is null)
            {
                var ex = Assert.Throws<NotSupportedException>(() => testValue.GetNonNullOrThrow(() => throw new NotSupportedException("custom msg")));
                Assert.IsType<NotSupportedException>(ex);
            }
            else
            {
                string newValue = testValue.GetNonNullOrThrow(() => throw new NotSupportedException("custom msg"));
                Assert.Equal(testValue, newValue);
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
                Assert.Equal(newValue, testValue);
            }
            else
            {
                var ex = Assert.Throws<ArgumentException>(() => testValue.GetValidatedOrThrow(func, nameof(testValue)));
                Assert.IsType<ArgumentException>(ex);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        public void TestGetTextOrThrow(string? testValue)
        {
            if(testValue is null)
            {
                var ex = Assert.Throws<ArgumentException>(() => testValue.GetTextOrThrow(nameof(testValue)));
                Assert.IsType<ArgumentException>(ex);
            }
            else
            {
                string newValue = testValue.GetTextOrThrow(nameof(testValue));
                Assert.Equal(newValue, testValue);
            }
        }
    }
}
