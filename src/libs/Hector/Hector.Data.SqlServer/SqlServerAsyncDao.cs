using Hector.Core;
using Hector.Data.DataReaders;
using Hector.Data.Entities;
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

        public override async Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = GetDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                IDataReader reader;
                if (items is T[])
                {
                    reader = new ArrayDataReader<T>(items.ToArray());
                }
                else
                {
                    reader = new EnumerableDataReader<T>(items);
                }

                using SqlBulkCopy bcp = new(connection as SqlConnection);
                bcp.DestinationTableName = tableName ?? EntityHelper.GetEntityTableName<T>().GetNonNullOrThrow(nameof(EntityHelper.GetEntityTableName));
                bcp.BatchSize = batchSize;
                bcp.BulkCopyTimeout = timeoutInSeconds;

                await bcp.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }

            return 0;
        }
    }
}
