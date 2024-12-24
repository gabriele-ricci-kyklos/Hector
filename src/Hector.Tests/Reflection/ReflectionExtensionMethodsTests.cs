using FastMember;
using FluentAssertions;
using Hector;
using Hector.Reflection;
using System.Data;
using System.Diagnostics;

namespace Hector.Tests.Reflection
{
    public class ReflectionExtensionMethodsTests
    {
        public class BaseEntity
        {
            public string? Name { get; set; }
        }

        public class ConcreteEntity : BaseEntity
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

        [Serializable]
        public class Entity
        {
            [Ordinal(2)]
            public int Dosage { get; set; }
            [Ordinal(1)]
            public string? Drug { get; set; }
            [Ordinal(3)]
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

        public class Entity4
        {
            public int Dosage { get; set; }
            public string? Drug { get; set; }
            public string? Diagnosis { get; set; }
            public DateTime? Date { get; set; }

            public Entity4(int dosage, string? drug, string? diagnosis, DateTime? date)
            {
                Dosage = dosage;
                Drug = drug;
                Diagnosis = diagnosis;
                Date = date;
            }
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
        public void TestGetPropertyInfoList()
        {
            var props = typeof(Entity).GetPropertyInfoList(nameof(Entity.Date));
            string[] expetedProps = [nameof(Entity.Dosage), nameof(Entity.Drug), nameof(Entity.Diagnosis)];

            props
                .Should().NotBeNull()
                .And.Equal(expetedProps, (x, y) => x.Name == y);
        }

        [Fact]
        public void TestGetTypeHierarchy()
        {
            typeof(ConcreteEntity).GetTypeHierarchy()
                .Should().NotBeNull().And.HaveCount(2);
        }

        [Fact]
        public void TestGetMemberList()
        {
            var result = typeof(Entity).GetMemberList(nameof(Entity.Date).AsArray());
            result.Should().NotBeNull().And.HaveCount(3);
        }

        [Fact]
        public void TestGetHierarchicalOrderedPropertyList()
        {
            string[] expetedProps = [nameof(BaseEntity.Name), nameof(ConcreteEntity.Dosage), nameof(ConcreteEntity.Drug), nameof(ConcreteEntity.Diagnosis)];

            var result = typeof(ConcreteEntity).GetHierarchicalOrderedPropertyList(nameof(ConcreteEntity.Date).AsArray());

            result.Should().NotBeNull();

            var names = result.Select(x => x.Name);

            names.Should().ContainInOrder(expetedProps);
        }

        [Fact]
        public void TestGetMemberValues()
        {
            TypeAccessor acc = TypeAccessor.Create(typeof(Entity));
            
            string[] expetedProps = [nameof(Entity.Diagnosis), nameof(Entity.Dosage), nameof(Entity.Drug)]; //order matters
            object[] expectedValues = [50, "Drug Z", "Problem Z"];

            Entity e = new()
            {
                Dosage = (int)expectedValues[0],
                Drug = (string)expectedValues[1],
                Diagnosis = (string)expectedValues[2],
                Date = DateTime.Now
            };

            var propertyValues = e.GetMemberValues(typeAccessor: acc, propertiesToExclude: nameof(Entity.Date).AsArray());

            propertyValues.Should().NotBeNull();
            propertyValues.Keys.Should().Contain(expetedProps);
            propertyValues.Values.Should().Contain(expectedValues);
        }

        [Fact]
        public void TestGetPropertyValues()
        {
            string[] expetedProps = [nameof(ConcreteEntity.Name), nameof(ConcreteEntity.Dosage), nameof(ConcreteEntity.Drug), nameof(ConcreteEntity.Diagnosis)]; //order matters
            object[] expectedValues = ["Hector", 50, "Drug Z", "Problem Z"];

            ConcreteEntity e = new()
            {
                Name = (string)expectedValues[0],
                Dosage = (int)expectedValues[1],
                Drug = (string)expectedValues[2],
                Diagnosis = (string)expectedValues[3],
                Date = DateTime.Now
            };

            var propertyValues = e.GetPropertyValues(propertiesToExclude: nameof(Entity.Date).AsArray());

            propertyValues.Should().NotBeNull();
            propertyValues.Keys.Should().ContainInOrder(expetedProps);
            propertyValues.Values.Should().ContainInOrder(expectedValues);
        }

        [Fact]
        public void TestSetMemberValue()
        {
            Entity e = new() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = DateTime.Now };
            int newDosage = 30;
            e.SetMemberValue("Dosage", newDosage);
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

            equals = PropertiesComparer.CompareProperties(obj, obj2, ["Dosage", "Drug"]);
            equals.Should().BeTrue();
        }

        [Fact]
        public void TestObjectCreationBenchmark()
        {
            Type type = typeof(Entity);
            var ctorDelegate = ObjectActivator.CreateILConstructorDelegate(type);
            var exprDelegate = ObjectActivator.CreateExpressionConstructorDelegate(type);
            int cycles = 1000000;

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < cycles; ++i)
            {
                _ = new Entity();
            }
            stopwatch.Stop();
            var newOpElapsed = stopwatch.Elapsed;
            stopwatch.Restart();
            for (int i = 0; i < cycles; ++i)
            {
                _ = ctorDelegate();
            }
            stopwatch.Stop();
            var ctorDelElapsed = stopwatch.Elapsed;
            stopwatch.Restart();
            for (int i = 0; i < cycles; ++i)
            {
                _ = ctorDelegate();
            }
            stopwatch.Stop();
            var exprDelElapsed = stopwatch.Elapsed;
            stopwatch.Restart();
            for (int i = 0; i < cycles; ++i)
            {
                _ = Activator.CreateInstance(type);
            }
            stopwatch.Stop();
            var activElapsed = stopwatch.Elapsed;
            stopwatch.Stop();
        }

        [Fact]
        public void TestObjectCreationIL()
        {
            object obj = ObjectActivator.CreateInstanceIL(typeof(Entity));
            Entity obj2 = ObjectActivator.CreateInstanceIL<Entity>();

            obj.GetType().Should().Be(typeof(Entity));
            obj2.GetType().Should().Be(typeof(Entity));
        }

        [Fact]
        public void TestObjectCreationExpression()
        {
            object obj = ObjectActivator.CreateInstanceExpression(typeof(Entity));
            Entity obj2 = ObjectActivator.CreateInstanceExpression<Entity>();

            obj.GetType().Should().Be(typeof(Entity));
            obj2.GetType().Should().Be(typeof(Entity));
        }

        //[Fact]
        //public void TestDynamicCtor()
        //{
        //    var instance =
        //        ObjectActivator
        //            .CreateInstance(typeof(Entity4), [1, "a", "b", null]);
        //}
    }
}
