using Hector;
using Hector.Data;
using Hector.Data.Oracle;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Hector.Tests.Data
{
    public class OracleTests
    {
        private static IAsyncDao NewAsyncDao()
        {
            AsyncDaoOptions options = new("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=172.16.10.53)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SID=rmxora)));User Id=RMX;Password=RMX;", "RMX", false);
            IAsyncDaoHelper daoHelper = new OracleAsyncDaoHelper(options.IgnoreEscape);
            IDbConnectionFactory connectionFactory = new OracleDbConnectionFactory(options.ConnectionString);
            IAsyncDao dao = new OracleAsyncDao(options, daoHelper, connectionFactory);
            return dao;
        }

        [Fact]
        public async Task TestOracleBulkInsertRaw()
        {
            IAsyncDao dao = NewAsyncDao();
            using OracleConnection conn = new(dao.ConnectionString);
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
        public async Task TestOracleBulkInsert()
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
        public async Task TestOracleUpsert()
        {
            IAsyncDao dao = NewAsyncDao();

            BulkItem[] items =
                Enumerable
                    .Range(0, 1000)
                    .Select(x => new BulkItem { Id = x, Code = "asd" })
                    .ToArray();

            await dao.ExecuteUpsertAsync(items);
        }
    }
}
