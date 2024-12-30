using FastMember;
using Hector;
using Hector.Data.Entities;
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

        protected override DbConnection GetDbConnection() => new OracleConnection(ConnectionString);

        public override async Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            //Va creato un query builder con una insert into con i nomi campi e parametri dell'entità
            //Va creata forse una logica common per realizzarlo
            //Poi settare il paramentro ArrayBindCount dentro OracleCommand con il count delle entità da inserire
            //Ed eseguire con ExecuteNonQueryAsync

            Type type = typeof(T);

            EntityPropertyInfo[] fieldInfoList =
                EntityHelper
                    .GetEntityPropertyInfoList(type)
                    .OrderBy(x => x.ColumnOrder)
                    .ToArray();

            Dictionary<string, List<object>> dataMap = [];
            TypeAccessor typeAccessor = TypeAccessor.Create(type);

            string fieldNames =
                fieldInfoList
                    .Select(x => _daoHelper.EscapeFieldName(x.ColumnName))
                    .StringJoin(", ");

            foreach (T item in items)
            {
                foreach (EntityPropertyInfo entityPropertyInfo in fieldInfoList)
                {
                    object value = typeAccessor[item, entityPropertyInfo.PropertyName];

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

            using DbConnection dbConnection = GetDbConnection();
            using DbCommand cmd = dbConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = timeoutInSeconds;

            List<string> paramNames = new(dataMap.Count);
            foreach (var item in dataMap)
            {
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = _daoHelper.BuildParameterName(item.Key);
                param.DbType = BaseAsyncDaoHelper.MapTypeToDbType(item.Value.First().GetType());
                param.Value = item.Value.ToArray();
                cmd.Parameters.Add(param);
                paramNames.Add(param.ParameterName);
            }

            tableName ??= EntityHelper.GetEntityTableName(type);
            string query = GetInsertIntoCommandText(Schema, tableName, fieldNames, paramNames.StringJoin(", "));
            cmd.CommandText = query;

            (cmd as OracleCommand)!.ArrayBindCount = items.Count();

            int affectedRecords = await ExecuteNonQueryAsync(dbConnection, cmd, cancellationToken).ConfigureAwait(false);
            return affectedRecords;
        }

        protected override string GetInsertIntoCommandText(string? schema, string tableName, string fieldNames, string paramNames) =>
            $" INSERT /*+ APPEND */ INTO {schema}{tableName} ({fieldNames}) VALUES ({paramNames})";

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

            var fieldNames =
                entityDefinition
                    .PropertyInfoList
                    .Select(x => _daoHelper.EscapeFieldName(x.ColumnName));

            string xmlEntityDefinition = serializer.SerializeEntityDefinition(Schema);

            string updateText =
                "UPDATE SET "
                + entityDefinition
                    .PropertyInfoList
                    .Select(x => x.ColumnName)
                    .Except(pkFields)
                    .Select(x =>
                    {
                        string f = _daoHelper.EscapeFieldName(x);
                        return $"dst.{x} = src.{x}";
                    })
                    .StringJoin(", ");

            string insertText =
                $"INSERT ({fieldNames.StringJoin(", ")}) VALUES ({fieldNames.Select(x => $"src.{x}").StringJoin(",")})";

            string upsertText = $@"
                MERGE INTO {Schema}.{tableName} dst
                USING
                (
                    {xmlEntityDefinition}
                ) src
                ON ({joinCondition})
                WHEN MATCHED THEN
                    {updateText}
                WHEN NOT MATCHED THEN
                    {insertText}";

            using DbConnection dbConnection = GetDbConnection();
            using DbCommand cmd = dbConnection.CreateCommand();

            cmd.CommandText = upsertText;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = timeoutInSeconds;

            string xmlData = serializer.SerializeEntityValues(items);

            OracleParameter p =
                new()
                {
                    ParameterName = "xmlData",
                    OracleDbType = OracleDbType.Clob,
                    Value = xmlData
                };

            cmd.Parameters.Add(p);

            int affectedRecords = await ExecuteNonQueryAsync(dbConnection, cmd, cancellationToken).ConfigureAwait(false);
            return affectedRecords;
        }
    }
}
