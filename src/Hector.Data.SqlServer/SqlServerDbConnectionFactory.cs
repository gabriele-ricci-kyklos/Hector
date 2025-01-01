using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace Hector.Data.SqlServer
{
    public class SqlServerDbConnectionFactory(string connectionString) : BaseDbConnectionFactory(connectionString)
    {
        public override DbConnection NewDbConnection() => new SqlConnection(ConnectionString);
    }
}
