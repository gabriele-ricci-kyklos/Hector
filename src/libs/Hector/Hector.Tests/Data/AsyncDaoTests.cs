using Hector.Core.Reflection;
using Hector.Data;
using Hector.Data.DataReaders;
using Hector.Data.Entities;
using Hector.Data.Entities.Attributes;
using Hector.Data.Oracle;
using Hector.Data.Queries;
using Hector.Data.SqlServer;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Hector.Tests.Data
{
    public class AsyncDaoTests
    {
        public class BulkItem : IBaseEntity
        {
            [EntityPropertyInfo(ColumnName = "ID", DbType = PropertyDbType.Long, IsNullable = false, ColumnOrder = 10)]
            public long Id { get; set; }

            [EntityPropertyInfo(ColumnName = "CODE", DbType = PropertyDbType.String, IsNullable = false, ColumnOrder = 20)]
            public string? Code { get; set; }

            public string TableName => "BULK";

            public string Alias => "B";

            public bool IsView => false;
        }

        public class Result
        {
            [EntityPropertyInfo(ColumnName = "JobID", DbType = PropertyDbType.Long, IsNullable = false, ColumnOrder = 10)]
            public long JobId2 { get; set; }

            [EntityPropertyInfo(ColumnName = "MemberID", DbType = PropertyDbType.Long, IsNullable = false, ColumnOrder = 20)]
            public long MemberId { get; set; }

            [EntityPropertyInfo(ColumnName = "DateOfWork", DbType = PropertyDbType.DateTime, IsNullable = false, ColumnOrder = 30)]
            public DateTime DateOfWork { get; set; }

            [EntityPropertyInfo(ColumnName = "AmountTimeToInvoice", DbType = PropertyDbType.Decimal, IsNullable = true, ColumnOrder = 40)]
            public decimal? AmountTimeToInvoice { get; set; }

            [EntityPropertyInfo(ColumnName = "FreeAmountTime", DbType = PropertyDbType.Decimal, IsNullable = true, ColumnOrder = 50)]
            public decimal? FreeAmountTime { get; set; }

            [EntityPropertyInfo(ColumnName = "TimeNote", DbType = PropertyDbType.String, IsNullable = true, MaxLength = 4000, ColumnOrder = 60)]
            public string? TimeNote { get; set; }

            [EntityPropertyInfo(ColumnName = "ReasonID", DbType = PropertyDbType.Integer, IsNullable = false, ColumnOrder = 70)]
            public int ReasonId { get; set; }

            [EntityPropertyInfo(ColumnName = "Hours", DbType = PropertyDbType.Decimal, IsNullable = true, ColumnOrder = 80)]
            public decimal? Hours { get; set; }

            [EntityPropertyInfo(ColumnName = "LastChangeDate", DbType = PropertyDbType.DateTime, IsNullable = true, ColumnOrder = 90)]
            public DateTime? LastChangeDate { get; set; }

            [EntityPropertyInfo(ColumnName = "JobTimeId", DbType = PropertyDbType.Long, IsNullable = false, IsPrimaryKey = true, ColumnOrder = 100)]
            public long JobTimeId { get; set; }
        }

        class Result2
        {
            public int ReasonID { get; set; }
        }

        [Fact]
        public async Task TestSqlServerAsyncDao()
        {
            AsyncDaoOptions options = new("Data Source=kkritstgdb.kyklos.local;Initial Catalog=Remira_Dev_JEK;Persist Security Info=True;User Id=rit-stg-mix;Password=3VCjmnxQxJVnDQxpHg2s", "dbo", false);
            SqlServerAsyncDaoHelper daoHelper = new();
            SqlServerAsyncDao dao = new(options, daoHelper);

            IQueryBuilder queryBuilder =
                dao
                    .NewQueryBuilder()
                    .SetQuery("select * from JobTimes");

            var results = await dao.ExecuteSelectQueryAsync<Result>(queryBuilder);

            var properties = typeof(Result).GetHierarchicalOrderedPropertyList().Select(x => x.Name).ToArray();
            var reader = new EnumerableDataReader<Result>(results);

            using var conn = new SqlConnection(options.ConnectionString);
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
            AsyncDaoOptions options = new("Data Source=kkritstgdb.kyklos.local;Initial Catalog=Remira_Dev_JEK;Persist Security Info=True;User Id=rit-stg-mix;Password=3VCjmnxQxJVnDQxpHg2s;TrustServerCertificate=True", "dbo", false);

            const string sql = @"
DECLARE @tableType T_Divisions
SELECT * from @tableType
";

            using SqlConnection conn = new SqlConnection(options.ConnectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            SqlDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo);
            DataTable schemaTable = reader.GetSchemaTable();
        }

        [Fact]
        public async Task TestOracleBulkInsert()
        {
            using var conn = new OracleConnection("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=172.16.10.53)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SID=rmxora)));User Id=RMX;Password=RMX;");
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO BULK (ID, CODE) VALUES (:id, :code)";
            cmd.CommandType = CommandType.Text;
            cmd.ArrayBindCount = 3;

            OracleParameter pId = cmd.CreateParameter();
            pId.OracleDbType = OracleDbType.Int32;
            pId.Value = new int[] { 1, 2, 3 };
            cmd.Parameters.Add(pId);

            OracleParameter pCode = cmd.CreateParameter();
            pCode.OracleDbType = OracleDbType.Varchar2;
            pCode.Value = new string[] { "one", "two", "three" };
            cmd.Parameters.Add(pCode);

            await cmd.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task TestOracleAsyncDao()
        {
            AsyncDaoOptions options = new("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=172.16.10.53)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SID=rmxora)));User Id=RMX;Password=RMX;", string.Empty, false);
            OracleAsyncDaoHelper daoHelper = new();
            OracleAsyncDao dao = new(options, daoHelper);

            BulkItem[] items =
                Enumerable
                    .Range(0, 1000)
                    .Select(x => new BulkItem { Id = x, Code = x.ToString() })
                    .ToArray();

            await dao.ExecuteBulkCopyAsync(items);
        }
    }
}
