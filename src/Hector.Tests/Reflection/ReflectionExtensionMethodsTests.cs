using FastMember;
using FluentAssertions;
using Hector.Core;
using Hector.Core.Reflection;
using System.Data;

namespace Hector.Tests.Reflection
{
    public class ReflectionExtensionMethodsTests
    {
        public class BaseEntity
        {
        }

        [Serializable]
        public class Entity : BaseEntity
        {
            public int Dosage { get; set; }
            public string? Drug { get; set; }
            public string? Diagnosis { get; set; }
            public DateTime? Date { get; set; }

            public override int GetHashCode() => HashCode.Combine(Dosage, Drug, Diagnosis, Date);

            public override bool Equals(object? obj)
            {
                if (obj is not Entity e)
                {
                    return false;
                }

                return Dosage.Equals(e.Dosage)
                    && (Drug.IsNullOrBlankString() || Drug.Equals(e.Drug))
                    && (Diagnosis.IsNullOrBlankString() || Diagnosis.Equals(e.Diagnosis))
                    && Date.Equals(e.Date);
            }
        }

        public class Entity2
        {
            public int Dosage { get; set; }
            public string? Drug { get; set; }
            public string? Diagnosis { get; set; }
            public DateTime? Date { get; set; }
        }

        public class Entity3
        {
            public int Dosage { get; set; }
            public string? Drug { get; set; }
            public string? Diagnosis { get; set; }
            public DateTime? Date { get; set; }
        }

        [Fact]
        public void TestToEntityList()
        {
            DataTable table = new();
            table.Columns.Add("Dosage", typeof(int));
            table.Columns.Add("Drug", typeof(string));
            table.Columns.Add("Diagnosis", typeof(string));
            table.Columns.Add("Date", typeof(DateTime));

            DateTime date = DateTime.Now;
            table.Rows.Add(25, "Drug A", "Disease A", date);
            table.Rows.Add(50, "Drug Z", "Problem Z", date);
            table.Rows.Add(10, "Drug Q", "Disorder Q", date);
            table.Rows.Add(21, "Medicine A", "Diagnosis A", date);

            Entity[] entities = table.ToEntityList<Entity>().ToArray();

            entities.Should().HaveCount(4).And.HaveElementAt(1, new Entity { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = date });
        }

        [Fact]
        public void TestGetPropertiesForType()
        {
            var props = typeof(Entity).GetPropertiesForType(nameof(Entity.Date));
            string[] expetedProps = [nameof(Entity.Dosage), nameof(Entity.Drug), nameof(Entity.Diagnosis)];

            props
                .Should().NotBeNull()
                .And.Equal(expetedProps, (x, y) => x.Name == y);
        }

        [Fact]
        public void TestGetTypeHierarchy()
        {
            typeof(Entity).GetTypeHierarchy()
                .Should().NotBeNull().And.HaveCount(2);
        }

        [Fact]
        public void TestGetUnorderedPropertyList()
        {
            typeof(Entity).GetUnorderedPropertyList(nameof(Entity.Date).AsArray())
                .Should().NotBeNull().And.HaveCount(3);
        }

        [Fact]
        public void TestGetOrderedPropertyList()
        {
            string[] expetedProps = [nameof(Entity.Dosage), nameof(Entity.Drug), nameof(Entity.Diagnosis)];

            typeof(Entity).GetOrderedPropertyList(nameof(Entity.Date).AsArray())
                .Should().NotBeNull()
                .And.Equal(expetedProps, (x, y) => x.Member.Name == y);
        }

        [Fact]
        public void TestGetPropertyValues()
        {
            TypeAccessor acc = TypeAccessor.Create(typeof(Entity));
            Entity e = new() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = DateTime.Now };
            string[] expetedProps = [nameof(Entity.Diagnosis), nameof(Entity.Dosage), nameof(Entity.Drug)]; //order matters
            var propertyValues = e.GetPropertyValues(typeAccessor: acc, propertiesToExclude: nameof(Entity.Date).AsArray());

            propertyValues
                .Should().NotBeNull()
                .And.Equal(expetedProps, (x, y) => x.Key == y && x.Value.Equals(acc[e, x.Key]));
        }

        [Fact]
        public void TestSetPropertyOrFieldValue()
        {
            Entity e = new() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = DateTime.Now };
            int newDosage = 30;
            e.SetPropertyOrFieldValue("Dosage", newDosage);
            e.Dosage.Should().Be(newDosage);
        }

        [Fact]
        public void TestIsSimpleType()
        {
            typeof(int).IsSimpleType().Should().BeTrue();
        }

        [Fact]
        public void TestGetNonNullableType()
        {
            typeof(int?).GetNonNullableType().Should().Be(typeof(int));
        }

        [Fact]
        public void TestIsNullableType()
        {
            typeof(int?).IsNullableType().Should().BeTrue();
        }

        [Fact]
        public void TestIsNumericType()
        {
            typeof(int).IsNumericType().Should().BeTrue();
        }

        [Fact]
        public void TestIsTypeTuple()
        {
            typeof(Tuple<int, int>).IsTypeTuple().Should().BeTrue();
        }

        [Fact]
        public void TestIsTypeValueTuple()
        {
            typeof(ValueTuple<int, int>).IsTypeValueTuple().Should().BeTrue();
        }

        [Fact]
        public void TestHasAttribute()
        {
            typeof(Entity).HasAttribute<SerializableAttribute>().Should().BeTrue();
        }

        [Fact]
        public void TestCopyPropertyValues()
        {
            Entity2 source = new() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = DateTime.Now };
            Entity3 dest = new();
            source.CopyPropertyValues(dest, propertiesToExclude: nameof(Entity.Date).AsArray());

            source.Date = null;
            dest.Should().BeEquivalentTo(source);
        }

        [Fact]
        public void TestCompareProperties()
        {
            Entity obj = new() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = DateTime.Now };
            Entity2 obj2 = new() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = obj.Date };

            bool equals = PropertiesComparer.CompareProperties(obj, obj2);
            equals.Should().BeTrue();

            equals = PropertiesComparer.CompareProperties(obj, obj2, [ "Dosage", "Drug" ]);
            equals.Should().BeTrue();
        }
    }
}
