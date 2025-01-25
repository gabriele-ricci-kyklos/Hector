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
            string connectionString = File.ReadAllText("..\\..\\..\\..\\..\\data\\TestConnectionStrings\\connstring_sqlserver.txt").GetNonNullOrThrow(nameof(connectionString));
            AsyncDaoOptions options = new(connectionString, "dbo", false);
            IAsyncDaoHelper daoHelper = new SqlServerAsyncDaoHelper(options.IgnoreEscape);
            IDbConnectionFactory connectionFactory = new SqlServerDbConnectionFactory(options.ConnectionString);
            IAsyncDao dao = new SqlServerAsyncDao(options, daoHelper, connectionFactory);
            return dao;
        }
    }
}
