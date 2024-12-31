using Hector.Data.Entities;
using Hector.Data.Queries;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Data.Oracle
{
    public class OracleAsyncDao : BaseAsyncDao
    {
        public OracleAsyncDao(AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper)
            : base(options, asyncDaoHelper)
        {
        }

        protected override DbConnection NewDbConnection() => new OracleConnection(ConnectionString);

        public override async Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            EntityDefinition<T> entityDefinition = new();

            Dictionary<string, List<object>> dataMap = [];

            string fieldNames =
                entityDefinition
                    .PropertyInfoList
                    .Select(x => _daoHelper.EscapeValue(x.ColumnName))
                    .StringJoin(", ");

            foreach (T item in items)
            {
                foreach (EntityPropertyInfo entityPropertyInfo in entityDefinition.PropertyInfoList)
                {
                    object value = entityDefinition.TypeAccessor[item, entityPropertyInfo.PropertyName];

                    if (dataMap.TryGetValue(entityPropertyInfo.ColumnName, out List<object>? values))
                    {
                        values.Add(value);
                    }
                    else
                    {
                        dataMap.Add(entityPropertyInfo.ColumnName, [value]);
                    }
                }
            }

            Dictionary<string, SqlParameter> parametersMap = new(dataMap.Count);
            foreach (var item in dataMap)
            {
                string paramName = _daoHelper.BuildParameterName(item.Key);
                Type type = item.Value.First().GetType();
                object value = item.Value.ToArray();

                SqlParameter param = new(type, paramName, value);
                parametersMap.Add(param.Name, param);
            }

            tableName ??= EntityHelper.GetEntityTableName(entityDefinition.Type);

            string query = $" INSERT /*+ APPEND */ INTO {Schema}{tableName} ({fieldNames}) VALUES ({parametersMap.Keys.StringJoin(", ")})";

            AsyncDaoCommand cmd = new(query, parametersMap.Values.ToArray());
            Action<DbCommand> commandFx = cmd => (cmd as OracleCommand)!.ArrayBindCount = items.Count();

            int affectedRecords = await ExecuteNonQueryCoreAsync(cmd, commandFx, timeoutInSeconds, cancellationToken).ConfigureAwait(false);
            return affectedRecords;
        }

        public override async Task<int> ExecuteUpsertAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            EntityDefinition<T> entityDefinition = new();
            EntityXMLSerializer<T> serializer = new(entityDefinition, _daoHelper);

            tableName ??= entityDefinition.TableName;

            string[] pkFields =
                entityDefinition.PrimaryKeyFields
                ?? throw new NotSupportedException($"No primary key fields found in entity type {entityDefinition.Type.FullName}, unable to perform the upsert");

            string joinCondition =
                pkFields
                    .Select(x => $"dst.{x} = src.{x}")
                    .StringJoin(" AND ");

            string xmlEntityDefinition = serializer.SerializeEntityDefinition(Schema);

            string updateText =
                "UPDATE SET "
                + entityDefinition
                    .PropertyInfoList
                    .Select(x => x.ColumnName)
                    .Except(pkFields)
                    .Select(x =>
                    {
                        string f = _daoHelper.EscapeValue(x);
                        return $"dst.{x} = src.{x}";
                    })
                    .StringJoin(", ");

            var fieldNames =
                entityDefinition
                    .PropertyInfoList
                    .Select(x => _daoHelper.EscapeValue(x.ColumnName));

            string insertText =
                $"INSERT ({fieldNames.StringJoin(", ")}) VALUES ({fieldNames.Select(x => $"src.{x}").StringJoin(",")})";

            string upsertText = $@"
                MERGE INTO {Schema}{_daoHelper.EscapeValue(tableName)} dst
                USING
                (
                    {xmlEntityDefinition}
                ) src
                ON ({joinCondition})
                WHEN MATCHED THEN
                    {updateText}
                WHEN NOT MATCHED THEN
                    {insertText}";

            string xmlData = serializer.SerializeEntityValues(items);

            OracleParameter p =
                new()
                {
                    ParameterName = "xmlData",
                    OracleDbType = OracleDbType.Clob,
                    Value = xmlData
                };

            AsyncDaoCommand cmd = new(upsertText, [new SqlParameter(p)]);

            int affectedRecords = await ExecuteNonQueryCoreAsync(cmd, null, timeoutInSeconds, cancellationToken).ConfigureAwait(false);
            return affectedRecords;
        }
    }
}
