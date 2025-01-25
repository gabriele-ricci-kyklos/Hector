using FluentAssertions;
using Hector;
using Hector.Data;
using Hector.Data.Oracle;
using Hector.Data.SqlServer;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Hector.Tests.Data
{
    public class OracleTests
    {
        private static IAsyncDao NewAsyncDao()
        {
            string connectionString = File.ReadAllText("..\\..\\..\\..\\..\\data\\TestConnectionStrings\\connstring_sqlserver.txt").GetNonNullOrThrow(nameof(connectionString));
            AsyncDaoOptions options = new(connectionString, "dbo", false);
            IAsyncDaoHelper daoHelper = new OracleAsyncDaoHelper(options.IgnoreEscape);
            IDbConnectionFactory connectionFactory = new OracleDbConnectionFactory(options.ConnectionString);
            IAsyncDao dao = new OracleAsyncDao(options, daoHelper, connectionFactory);
            return dao;
        }

        [Fact]
        public void TestSqlServerAsyncDaoFactory()
        {
            IAsyncDao dao = AsyncDaoFactory.CreateAsyncDao(OracleProvider.Name, new("asd", "dbo", false));
            dao.Should().NotBeNull();
        }
    }
}
