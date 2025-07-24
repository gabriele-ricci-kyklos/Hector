using FluentAssertions;

namespace Hector.Tests.Core.ExtensionMethods
{
    record Item
    {
        public int Value { get; set; }

        public Item(int value)
        {
            Value = value;
        }
    }

    public class CollectionsExtensionMethodsTests
    {
        [Theory]
        [InlineData(new int[] { 1, 2, 3 })]
        [InlineData(null)]

        public void TestIsNullOrEmptyList(int[]? arr)
        {
            bool outcome = arr.IsNullOrEmptyList();
            outcome.Should().Be(arr is null);
        }

        [Theory]
        [InlineData(new int[] { 1, 2, 3 })]
        [InlineData(null)]
        public void TestIsNotNullAndNotEmptyList(int[]? arr)
        {
            bool outcome = arr.IsNotNullAndNotEmptyList();
            outcome.Should().Be(arr is not null);
        }

        [Fact]
        public void TestToEmptyIfNull()
        {
            int[]? arr = null;
            var outcome = arr.ToEmptyIfNull();
            outcome.Should().BeEmpty();
        }

        [Fact]
        public void TestToNullIfEmpty()
        {
            int[] arr = [];
            var outcome = arr.ToNullIfEmpty();
            outcome.Should().BeNull();
        }

        [Fact]
        public void TestToEmptyListIfNull()
        {
            int[]? arr = null;
            List<int> outcome = arr.ToEmptyListIfNull();
            outcome.Should().BeEmpty();
        }

        [Fact]
        public void TestToEmptyArrayIfNull()
        {
            int[]? arr = null;
            int[] outcome = arr.ToEmptyArrayIfNull();
            outcome.Should().BeEmpty();
        }

        [Fact]
        public void TestToNullIfEmptyArray()
        {
            int[] arr = [];
            int[]? outcome = arr.ToNullIfEmptyArray();
            outcome.Should().BeNull();
        }

        [Fact]
        public void TestToNullIfEmptyList()
        {
            int[] arr = [];
            List<int>? outcome = arr.ToNullIfEmptyList();
            outcome.Should().BeNull();
        }

        [Fact]
        public void TestForEach()
        {
            Item[] arr = [new(1), new(2), new(3)];
            arr.ForEach(x => x.Value = 0);
            arr.Should().OnlyContain(x => x.Value == 0);
        }

        [Fact]
        public void TestForEachWithIndex()
        {
            Item[] arr = [new(1), new(2), new(3)];
            arr.ForEach((x, i) => x.Value = 0);
            arr.Should().OnlyContain(x => x.Value == 0);
        }

        [Fact]
        public void TestIsInWithParams()
        {
            bool outcome = 1.IsIn(1, 2, 3);
            outcome.Should().BeTrue();
        }

        [Fact]
        public void TestIsInWithParamsAndEqFx()
        {
            bool outcome = 2.IsIn((x, y) => x == y, 1, 2, 3);
            outcome.Should().BeTrue();
        }

        [Fact]
        public void TestIsInWithEnumerable()
        {
            int[] arr = [1, 2, 3];
            bool outcome = 1.IsIn(arr);
            outcome.Should().BeTrue();
        }

        [Fact]
        public void TestIsInWithEnumerableAndEqFx()
        {
            int[] arr = [1, 2, 3];
            bool outcome = 1.IsIn(arr, (x, y) => x == y);
            outcome.Should().BeTrue();
        }

        [Fact]
        public void TestAsList()
        {
            List<int> outcome = 1.AsList();
            outcome.Should()
                .BeOfType<List<int>>()
                .And.ContainSingle();
        }

        [Fact]
        public void TestAsArray()
        {
            int[] outcome = 1.AsArray();
            outcome.Should()
                .BeOfType<int[]>()
                .And.ContainSingle();
        }

        [Fact]
        public void TestAsArraySizeAndValues()
        {
            int i = 1, l = 2;
            int[] outcome = i.AsArray(l, true);
            outcome.Should()
                .BeOfType<int[]>()
                .And.HaveCount(l)
                .And.OnlyContain(x => x == i);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(null)]
        public void TestAsArrayOrNull(int? item)
        {
            bool itemIsNull = item is null;
            int?[]? outcome = item.AsArrayOrNull();

            if (itemIsNull)
            {
                outcome.Should().BeNull();
            }
            else
            {
                outcome.Should().NotBeNull()
                    .And.ContainSingle()
                    .Which.Should().Be(item);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(null)]
        public void TestAsListOrNull(int? item)
        {
            bool itemIsNull = item is null;
            List<int?>? outcome = item.AsListOrNull();

            if (itemIsNull)
            {
                outcome.Should().BeNull();
            }
            else
            {
                outcome.Should().NotBeNull()
                    .And.ContainSingle()
                    .Which.Should().Be(item);
            }
        }

        [Fact]
        public void TestShuffle()
        {
            int[] arr = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
            arr.Shuffle();
            arr.Should().NotBeInAscendingOrder();
        }

        [Fact]
        public void TestSplit()
        {
            int[] arr = [1, 2, 3, 4];
            var matrix = arr.Split(2);
            matrix.Should().AllSatisfy(x => x.Should().HaveCount(2));
        }

        [Fact]
        public void TestMergeDict()
        {
            Dictionary<int, int> dictSource = new()
            {
                {1, 1},
                {2, 2}
            };

            Dictionary<int, int> dictDest = new()
            {
                {1, 4},
                {2, 5},
                {3, 6}
            };

            var res = dictSource.MergeLeft(dictDest);
            res[1].Should().Be(1);
            res[2].Should().Be(2);
            res[3].Should().Be(6);

            res = dictSource.MergeRight(dictDest);
            res[1].Should().Be(4);
            res[2].Should().Be(5);
            res[3].Should().Be(6);
        }

        [Fact]
        public void TestMergeDictWithEmpty()
        {
            Dictionary<int, int> dictSource = new()
            {
                {1, 1},
                {2, 2}
            };

            Dictionary<int, int> dictDest = [];

            var res = dictSource.MergeLeft(dictDest);
            res[1].Should().Be(1);
            res[2].Should().Be(2);

            res = dictSource.MergeRight(dictDest);
            res[1].Should().Be(1);
            res[2].Should().Be(2);

            dictSource = [];
            dictDest = new()
            {
                {1, 1},
                {2, 2}
            };

            res = dictSource.MergeLeft(dictDest);
            res[1].Should().Be(1);
            res[2].Should().Be(2);

            res = dictSource.MergeRight(dictDest);
            res[1].Should().Be(1);
            res[2].Should().Be(2);
        }

        [Fact]
        public void TestAddRange()
        {
            HashSet<int> set = [];
            int[] items = [1, 2, 3];
            set.AddRange(items);

            set.Should()
                .NotBeEmpty()
                .And.ContainInOrder(items);
        }
    }
}
