using Hector;
using Hector.Data.DataReaders;
using Hector.Data.Entities;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Data.SqlServer
{
    public class SqlServerAsyncDao : BaseAsyncDao
    {
        public SqlServerAsyncDao(AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper, IDbConnectionFactory connectionFactory)
            : base(options, asyncDaoHelper, connectionFactory)
        {
        }

        public override async Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnectionContext connectionContext = NewConnectionContext();

            try
            {
                await connectionContext.OpenAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                using DbDataReader reader =
                    (items is T[])
                        ? new ArrayDbDataReader<T>(items.ToArray())
                        : new EnumerableDbDataReader<T>(items);

                using SqlBulkCopy bcp = new(connectionContext.DbConnection as SqlConnection);
                bcp.DestinationTableName = tableName ?? EntityHelper.GetEntityTableName<T>();
                bcp.BatchSize = batchSize;
                bcp.BulkCopyTimeout = timeoutInSeconds;

                await bcp.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);

                return bcp.RowsCopied;
            }
            finally
            {
                await connectionContext.CloseAsync().ConfigureAwait(false);
            }
        }

        public override async Task<int> ExecuteUpsertAsync<T>(IEnumerable<T> items, string? tableName = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            EntityDefinition<T> entityDefinition = new();

            tableName ??= entityDefinition.TableName;

            EntityPropertyInfo[] propertyInfoList =
                entityDefinition
                    .PropertyInfoList
                    .OrderBy(x => x.ColumnOrder)
                    .ToArray();

            string[] pkFields =
                entityDefinition
                    .PrimaryKeyFields
                    .ToNullIfEmptyArray()
                ?? throw new NotSupportedException($"No primary key fields found in entity type {entityDefinition.Type.FullName}, unable to perform the upsert");

            string joinCondition =
                pkFields
                    .Select(x => $"dst.{x} = src.{x}")
                    .StringJoin(" AND ");

            string updateText =
                "UPDATE SET "
                + entityDefinition
                    .PropertyInfoList
                    .Select(x => x.ColumnName)
                    .Except(pkFields)
                    .Select(x => $"dst.{x} = src.{x}")
                    .StringJoin(", ");

            var fieldNames =
                propertyInfoList
                    .Select(x => DaoHelper.EscapeValue(x.ColumnName));

            string insertText =
                $"INSERT ({fieldNames.StringJoin(", ")}) VALUES ({fieldNames.Select(x => $"src.{x}").StringJoin(",")})";

            string upsertText = $@"
                MERGE {Schema}{DaoHelper.EscapeValue(tableName)} as dst
                USING
                (
                    SELECT {fieldNames.StringJoin(",")}
                    FROM @sourceData
                ) AS src
                ON ({joinCondition})
                WHEN MATCHED THEN
                    {updateText}
                WHEN NOT MATCHED BY TARGET THEN
                    {insertText};";

            using EntityDbDataReader<T> dataReader = new(items, DaoHelper.GetNumericPrecision);

            SqlParameter p =
                new("sourceData", SqlDbType.Structured)
                {
                    TypeName = $"{Schema}T_{tableName}",
                    SqlDbType = SqlDbType.Structured,
                    Value = dataReader
                };

            AsyncDaoCommand cmd = new(upsertText, [new Queries.SqlParameter(p)]);

            int affectedRecords = await ExecuteNonQueryCoreAsync(cmd, null, timeoutInSeconds, cancellationToken).ConfigureAwait(false);
            return affectedRecords;
        }

        public override ITransactionalAsyncDao NewTransactionalAsyncDao(DbConnectionContext connectionContext) =>
            new SqlServerTransactionalAsyncDao(connectionContext, _options, DaoHelper, _connectionFactory);
    }
}
