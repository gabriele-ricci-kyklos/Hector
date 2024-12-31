using FluentAssertions;
using Hector.Data;
using Hector.Data.DataReaders;
using Hector.Data.Queries;
using Hector.Data.SqlServer;
using Hector.Reflection;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace Hector.Tests.Data
{
    public class SqlServerTests
    {
        private static IAsyncDao NewAsyncDao()
        {
            AsyncDaoOptions options = new("Data Source=kkritstgdb.kyklos.local;Initial Catalog=Remira_Dev_JEK;Persist Security Info=True;User Id=rit-stg-mix;Password=3VCjmnxQxJVnDQxpHg2s;TrustServerCertificate=True", "dbo", false);
            IAsyncDaoHelper daoHelper = new SqlServerAsyncDaoHelper(options.IgnoreEscape);
            IAsyncDao dao = new SqlServerAsyncDao(options, daoHelper);
            return dao;
        }

        [Fact]
        public async Task TestIAsyncDao()
        {
            IAsyncDao dao = NewAsyncDao();

            IQueryBuilder queryBuilder =
                dao
                    .NewQueryBuilder()
                    .SetQuery("select * from JobTimes");

            var results = await dao.ExecuteSelectQueryAsync<Result>(queryBuilder);

            var properties = typeof(Result).GetHierarchicalOrderedPropertyList().Select(x => x.Name).ToArray();
            var reader = new EnumerableDbDataReader<Result>(results);

            using var conn = new SqlConnection(dao.ConnectionString);
            await conn.OpenAsync();
            using (var bcp = new SqlBulkCopy(conn))
            {
                bcp.DestinationTableName = "JobTimes2";
                await bcp.WriteToServerAsync(reader);
            }
        }

        [Fact]
        public async Task TestSqlServerTableType()
        {
            IAsyncDao dao = NewAsyncDao();

            const string sql = @"
DECLARE @tableType T_Divisions
SELECT * from @tableType
";

            using SqlConnection conn = new SqlConnection(dao.ConnectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo);
            DataTable schemaTable = reader.GetSchemaTable();
        }

        [Fact]
        public async Task TestSqlServerUpsert()
        {
            IAsyncDao dao = NewAsyncDao();

            BulkItem[] items =
                Enumerable
                    .Range(0, 1000)
                    .Select(x => new BulkItem { Id = x, Code = "asd" })
                    .ToArray();

            await dao.ExecuteUpsertAsync(items);
        }

        [Fact]
        public async Task TestSqlServerBulkInsert()
        {
            IAsyncDao dao = NewAsyncDao();

            BulkItem[] items =
                Enumerable
                    .Range(0, 1000)
                    .Select(x => new BulkItem { Id = x, Code = x.ToString() })
                    .ToArray();

            await dao.ExecuteBulkCopyAsync(items);
        }

        [Fact]
        public async Task TestSqlServerScalar()
        {
            IAsyncDao dao = NewAsyncDao();

            int c = await dao.ExecuteScalarAsync<int>(dao.NewQueryBuilder().SetQuery("select count(*) from [BULK]"));
            c.Should().BePositive();
        }

        [Fact]
        public async Task TestTableTypeDefinition()
        {
            IAsyncDao dao = NewAsyncDao();
            TableTypeHelper tableTypeHelper = new(dao);
            await tableTypeHelper.CreateTableTypesForEntitiesAsync(Assembly.GetExecutingAssembly());
        }
    }
}
