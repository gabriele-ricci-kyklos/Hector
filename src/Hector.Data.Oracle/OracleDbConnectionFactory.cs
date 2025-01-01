using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace Hector.Data.Oracle
{
    public class OracleDbConnectionFactory(string connectionString) : BaseDbConnectionFactory(connectionString)
    {
        public override DbConnection NewDbConnection() => new OracleConnection(ConnectionString);
    }
}
