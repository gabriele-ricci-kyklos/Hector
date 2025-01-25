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
            AsyncDaoOptions options = File.ReadAllText("..\\..\\..\\..\\..\\data\\TestConnectionStrings\\connstring_sqlserver.txt").GetNonNullOrThrow(nameof(connectionString));
            IAsyncDaoHelper daoHelper = new OracleAsyncDaoHelper(options.IgnoreEscape);
            IDbConnectionFactory connectionFactory = new OracleDbConnectionFactory(options.ConnectionString);
            IAsyncDao dao = new OracleAsyncDao(options, daoHelper, connectionFactory);
            return dao;
        }
    }
}
