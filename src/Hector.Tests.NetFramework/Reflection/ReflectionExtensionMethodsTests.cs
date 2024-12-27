using FastMember;
using FluentAssertions;
using Hector.Reflection;
using System;
using System.Data;
using System.Linq;
using Xunit;

namespace Hector.Tests.NetFramework.Reflection
{
    public class ReflectionExtensionMethodsTests
    {
        [Fact]
        public void TestCompareProperties()
        {
            Entity obj = new Entity() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = DateTime.Now };
            Entity2 obj2 = new Entity2() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = obj.Date };

            bool equals = PropertiesComparer.CompareProperties(obj, obj2);
            equals.Should().BeTrue();

            equals = PropertiesComparer.CompareProperties(obj, obj2, new string[] { "Dosage", "Drug" });
            equals.Should().BeTrue();
        }

        [Fact]
        public void TestToEntityList()
        {
            DataTable table = new DataTable();
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
        public void TestCopyPropertyValues()
        {
            Entity2 source = new Entity2() { Dosage = 50, Drug = "Drug Z", Diagnosis = "Problem Z", Date = DateTime.Now };
            Entity3 dest = new Entity3();
            source.CopyPropertyValues(dest, propertiesToExclude: nameof(Entity.Date).AsArray());

            source.Date = null;
            dest.Should().BeEquivalentTo(source);
        }

        [Fact]
        public void TestGetHierarchicalOrderedPropertyList()
        {
            string[] expetedProps = new string[] { nameof(BaseEntity.Name), nameof(ConcreteEntity.Dosage), nameof(ConcreteEntity.Drug), nameof(ConcreteEntity.Diagnosis) };

            var result = typeof(ConcreteEntity).GetHierarchicalOrderedPropertyList(nameof(ConcreteEntity.Date).AsArray());

            result.Should().NotBeNull();

            var names = result.Select(x => x.Name);

            names.Should().ContainInOrder(expetedProps);
        }
    }

    public class Entity
    {
        [Ordinal(2)]
        public int Dosage { get; set; }
        [Ordinal(1)]
        public string Drug { get; set; }
        [Ordinal(3)]
        public string Diagnosis { get; set; }
        public DateTime? Date { get; set; }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + Dosage.GetHashCode();
                hash = hash * 23 + Drug.GetHashCode();
                hash = hash * 23 + Diagnosis.GetHashCode();
                hash = hash * 23 + Date.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Entity e)
            {
                return Dosage.Equals(e.Dosage)
                && (Drug.IsNullOrBlankString() || Drug.Equals(e.Drug))
                && (Diagnosis.IsNullOrBlankString() || Diagnosis.Equals(e.Diagnosis))
                && Date.Equals(e.Date);
            }

            return false;
        }
    }

    public class Entity2
    {
        public int Dosage { get; set; }
        public string Drug { get; set; }
        public string Diagnosis { get; set; }
        public DateTime? Date { get; set; }
    }

    public class Entity3
    {
        public int Dosage { get; set; }
        public string Drug { get; set; }
        public string Diagnosis { get; set; }
        public DateTime? Date { get; set; }
    }

    public class BaseEntity
    {
        public string Name { get; set; }
    }

    public class ConcreteEntity : BaseEntity
    {
        public int Dosage { get; set; }
        public string Drug { get; set; }
        public string Diagnosis { get; set; }
        public DateTime? Date { get; set; }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + Dosage.GetHashCode();
                hash = hash * 23 + Drug.GetHashCode();
                hash = hash * 23 + Diagnosis.GetHashCode();
                hash = hash * 23 + Date.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Entity e)
            {
                return Dosage.Equals(e.Dosage)
                && (Drug.IsNullOrBlankString() || Drug.Equals(e.Drug))
                && (Diagnosis.IsNullOrBlankString() || Diagnosis.Equals(e.Diagnosis))
                && Date.Equals(e.Date);
            }

            return false;
        }
    }
}
