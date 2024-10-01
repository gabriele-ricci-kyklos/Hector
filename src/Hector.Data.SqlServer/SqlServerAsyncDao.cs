using Hector.Data.DataReaders;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Data.SqlServer
{
    public class SqlServerAsyncDao : BaseAsyncDao
    {
        public SqlServerAsyncDao(AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper)
            : base(options, asyncDaoHelper)
        {
        }

        protected override DbConnection GetDbConnection() => new SqlConnection(ConnectionString);

        public override async Task ExecuteBulkCopyAsync<T>(IEnumerable<T> items, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = GetDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            IDataReader reader;
            if (items is T[])
            {
                reader = new ArrayDataReader<T>(items.ToArray());
            }
            else
            {
                reader = new EnumerableDataReader<T>(items);
            }

            T firstItem = items.First();
            using SqlBulkCopy bcp = new(connection as SqlConnection);
            bcp.DestinationTableName = firstItem.TableName;
            bcp.BatchSize = batchSize;
            bcp.BulkCopyTimeout = timeoutInSeconds;

            await bcp.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);

            await connection.CloseAsync().ConfigureAwait(false);
        }
    }
}
