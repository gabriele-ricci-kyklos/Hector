using Hector.Core;
using Hector.Data.DataReaders;
using Hector.Data.Entities;
using System;
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

                return 0;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        public override async Task<int> ExecuteUpsertAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            Type type = typeof(T);

            tableName ??= EntityHelper.GetEntityTableName(type).GetNonNullOrThrow(nameof(EntityHelper.GetEntityTableName));

            string[] pkFields =
                EntityHelper
                    .GetPrimaryKeyFields(type)
                    .ToNullIfEmptyArray()
                ?? throw new NotSupportedException($"No primary key fields found in entity type {type.FullName}, unable to perform the upsert");

            string joinCondition =
                pkFields
                    .Select(x => $"dst.{x} = src.{x}")
                    .StringJoin(" AND ");

            EntityPropertyInfo[] fieldInfoList =
                EntityHelper
                    .GetEntityPropertyInfoList(type)
                    .OrderBy(x => x.ColumnOrder)
                    .ToArray();

            var fieldNames =
                fieldInfoList
                    .Select(x => _daoHelper.EscapeFieldName(x.ColumnName));

            string updateText =
                "UPDATE SET "
                + fieldNames
                    .Select(x => $"src.{x} = dst.{x}")
                    .StringJoin(", ");

            string insertText =
                $"INSERT ({fieldNames.StringJoin(", ")}) VALUES ({fieldNames.Select(x => $"src.{x}").StringJoin(",")})";

            string upsertText = $@"
                MERGE {Schema}.{tableName} as dst
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

            using EntityDataReader dataReader = new(type, items, _daoHelper.GetNumericPrecision);

            using DbConnection dbConnection = GetDbConnection();
            using DbCommand cmd = dbConnection.CreateCommand();

            cmd.CommandText = upsertText;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = timeoutInSeconds;

            SqlParameter p =
                new("sourceData", SqlDbType.Structured)
                {
                    TypeName = $"{Schema}.T_{tableName}",
                    SqlDbType = SqlDbType.Structured,
                    Value = dataReader
                };

            cmd.Parameters.Add(p);

            int affectedRecords = await ExecuteNonQueryAsync(dbConnection, cmd, cancellationToken).ConfigureAwait(false);
            return affectedRecords;
        }
    }
}
