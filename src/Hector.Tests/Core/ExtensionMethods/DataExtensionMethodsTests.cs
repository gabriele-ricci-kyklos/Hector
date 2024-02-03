using FluentAssertions;
using Hector.Core;
using System.Data;

namespace Hector.Tests.Core.ExtensionMethods
{
    public class DataExtensionMethodsTests
    {
        class Entity
        {
            public int Dosage { get; set; }
            public string? Drug { get; set; }
            public string? Diagnosis { get; set; }
            public DateTime? Date { get; set; }

            public override int GetHashCode() => HashCode.Combine(Dosage, Drug, Diagnosis, Date);

            public override bool Equals(object? obj)
            {
                if(obj is not Entity e)
                {
                    return false;
                }

                return Dosage.Equals(e.Dosage)
                    && (Drug.IsNullOrBlankString() || Drug.Equals(e.Drug))
                    && (Diagnosis.IsNullOrBlankString() || Diagnosis.Equals(e.Diagnosis))
                    && Date.Equals(e.Date);
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
    }
}
