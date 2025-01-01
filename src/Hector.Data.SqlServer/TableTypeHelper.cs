using Hector.Data.Entities;
using Hector.Data.Entities.Attributes;
using Hector.Data.Queries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Data.SqlServer
{
    public class TableTypeHelper(IAsyncDao asyncDao)
    {
        private readonly IAsyncDao _dao = asyncDao;

        public string CreateTableTypeSqlDefinition<T>(string? tableName = null) where T : IBaseEntity =>
            CreateTableTypeSqlDefinition(new EntityDefinition<T>(), tableName);

        public string CreateTableTypeSqlDefinition<T>(EntityDefinition<T> entityDefinition, string? tableName = null)
            where T : IBaseEntity =>
            CreateTableTypeSqlDefinition(entityDefinition, tableName);

        public string CreateTableTypeSqlDefinition(EntityDefinition entityDefinition, string? tableName = null)
        {
            tableName ??= entityDefinition.TableName;
            string tableTypeName = _dao.DaoHelper.EscapeValue($"T_{tableName}");

            StringBuilder builder = new($"create type {tableTypeName} as table ({Environment.NewLine}");

            for (int i = 0; i < entityDefinition.PropertyInfoList.Length; ++i)
            {
                EntityPropertyInfo propertyInfo = entityDefinition.PropertyInfoList[i];

                string sqlType = _dao.DaoHelper.MapDbTypeToSqlType(propertyInfo);
                string sqlName = _dao.DaoHelper.EscapeValue(propertyInfo.ColumnName);
                string sqlNullable = propertyInfo.IsNullable ? "null" : "not null";

                builder.Append($"{sqlName} {sqlType} {sqlNullable}");
                if (i != entityDefinition.PropertyInfoList.Length - 1)
                {
                    builder.Append($",{Environment.NewLine}");
                }
            }

            builder.Append($"{Environment.NewLine})");

            return builder.ToString();
        }

        public async Task CreateTableTypesForEntitiesAsync(IEnumerable<Type> entityTypes, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            StringBuilder commandBuilder = new();

            foreach (Type entityType in entityTypes)
            {
                EntityDefinition entityDefinition = new(entityType);
                string createScript = CreateTableTypeSqlDefinition(entityDefinition);
                commandBuilder.Append(createScript);
                commandBuilder.AppendLine(";");
            }

            if (commandBuilder.Length == 0)
            {
                return;
            }

            //TODO: do in transaction in order to rollback all in case of errors
            await _dao
                .ExecuteNonQueryAsync(commandBuilder.ToString(), null, timeoutInSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task CreateTableTypesForEntitiesAsync(Assembly assembly, Func<Type, EntityInfoAttribute, bool>? predicate = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            var assemblies =
                assembly
                    .GetReferencedAssemblies()
                    .Select(Assembly.Load)
                    .Union([assembly])
                    .Distinct();

            var entityTypes =
                EntityHelper
                    .GetAllEntityTypesInAssemblyList(assemblies, predicate);

            Dictionary<string, Type> mapTableToTypeDict =
                entityTypes
                    .ToDictionary(x => EntityHelper.GetEntityTableName(x));

            TableTypeDetailModel[] currentDbItemDetailsModels =
                GetTableTypeDetailModelsFromAssemblies(entityTypes);

            TableTypeDetailModel[] existingTableTypeDetailsModels =
                await GetExistingTableTypesDetailsModels(mapTableToTypeDict).ConfigureAwait(false);

            var differences =
                currentDbItemDetailsModels
                    .Except(existingTableTypeDetailsModels);

            Type[] tableTypesDifferences =
                differences
                    .Select(x => x.DbItemType)
                    .Distinct()
                    .ToArray();

            await CreateTableTypesForEntitiesAsync(tableTypesDifferences, timeoutInSeconds, cancellationToken).ConfigureAwait(false);

            //if (tableTypesDifferences.Length > 0)
            //{
            //    await _dao
            //        .ExecuteTransactionAsync
            //        (async tdao =>
            //        {
            //            await tdao.DropTableTypesForEntitiesAsync(tableTypesDifferences).ConfigureAwait(false);
            //            await tdao.CreateSqlServerTypesFromLoadedAssembliesAsync(tableTypesDifferences).ConfigureAwait(false);
            //        })
            //        .ConfigureAwait(false);
            //}
        }

        private async Task<TableTypeDetailDbItem[]> GetExistingTableTypesDetailsAsync()
        {
            const string query = @"
select t.name as TableTypeName,
c.name as ColumnName,
st.name as ColumnType,
c.max_length as MaxLength,
c.is_nullable as IsNullable
from sys.table_types t
inner join sys.columns c on t.type_table_object_id = c.object_id
inner join sys.types st on c.system_type_id = st.system_type_id 
and c.user_type_id = st.user_type_id 
where t.is_table_type = 1";

            IQueryBuilder builder =
                _dao
                    .NewQueryBuilder(query);

            TableTypeDetailDbItem[] tableTypes =
                await _dao
                    .ExecuteSelectQueryAsync<TableTypeDetailDbItem>(builder)
                    .ConfigureAwait(false);

            return tableTypes;
        }

        private async Task<TableTypeDetailModel[]> GetExistingTableTypesDetailsModels(Dictionary<string, Type> mappingTableToTypeDict)
        {
            const string tableTypeInitialToken = "T_";

            TableTypeDetailDbItem[] existingTableTypeDetails =
                await GetExistingTableTypesDetailsAsync().ConfigureAwait(false);

            List<TableTypeDetailModel> existingTableTypeDetailsModels = [];

            foreach (TableTypeDetailDbItem e in existingTableTypeDetails)
            {
                string tableName =
                    e.TableTypeName.StartsWith(tableTypeInitialToken)
                    ? e.TableTypeName.Remove(0, 2)
                    : e.TableTypeName;

                Type type = MapSqlServerTypeToCSharpType(e.ColumnType);
                DbType columnType = BaseAsyncDaoHelper.MapTypeToDbType(type);

                int maxLength = -2;
                if (columnType == DbType.String)
                {
                    maxLength = e.MaxLength / 2;
                }

                TableTypeDetailModel model = new
                (
                    DbItemType: mappingTableToTypeDict.NetCoreGetValueOrDefault(tableName)!,
                    TableTypeName: tableName,
                    ColumnName: e.ColumnName,
                    ColumnType: columnType,
                    MaxLength: maxLength,
                    IsNullable: e.IsNullable
                );

                existingTableTypeDetailsModels.Add(model);
            }

            return existingTableTypeDetailsModels.ToArray();
        }

        private TableTypeDetailModel[] GetTableTypeDetailModelsFromAssemblies(IEnumerable<Type> currentDbItemTypes)
        {
            List<TableTypeDetailModel> currentDbItemDetails = [];

            foreach (Type dbItemType in currentDbItemTypes)
            {
                EntityDefinition entityDefinition = new(dbItemType);

                foreach (EntityPropertyInfo dbItemProperty in entityDefinition.PropertyInfoList)
                {
                    DbType columnType = BaseAsyncDaoHelper.MapTypeToDbType(dbItemProperty.Type);

                    int maxLength = -2;
                    if (columnType == DbType.String)
                    {
                        maxLength = dbItemProperty.MaxLength == -1 ? 0 : dbItemProperty.MaxLength;
                    }

                    TableTypeDetailModel model = new
                    (
                        DbItemType: dbItemType,
                        TableTypeName: entityDefinition.TableName,
                        ColumnName: dbItemProperty.ColumnName,
                        ColumnType: columnType,
                        MaxLength: maxLength,
                        IsNullable: dbItemProperty.IsNullable
                    );

                    currentDbItemDetails.Add(model);
                }
            }
            return currentDbItemDetails.ToArray();
        }

        private static Type MapSqlServerTypeToCSharpType(string sqlServerType) =>
            sqlServerType switch
            {
                "bit" => typeof(bool),
                "tinyint" => typeof(short),
                "nvarchar(MAX)" => typeof(string),
                "varchar(MAX)" => typeof(string),
                "datetime" => typeof(DateTime),
                "datetime2" => typeof(DateTime),
                "decimal" => typeof(decimal),
                "float" => typeof(double),
                "real" => typeof(float),
                "int" => typeof(int),
                "bigint" => typeof(long),
                "smallint" => typeof(short),
                "nvarchar" => typeof(string),
                "varchar" => typeof(string),
                "numeric" => typeof(double),
                _ => throw new NotSupportedException(),
            };
    }

    record TableTypeDetailModel(Type DbItemType, string TableTypeName, string ColumnName, DbType? ColumnType, int MaxLength, bool IsNullable);

#nullable disable

    [EntityInfo(TableName = "TableTypeDetailDbItem", IsView = true)]
    class TableTypeDetailDbItem : IBaseEntity
    {
        [EntityPropertyInfo(ColumnName = "TableTypeName", DbType = PropertyDbType.String, IsNullable = false, MaxLength = 128, ColumnOrder = 10)]
        public string TableTypeName { get; set; }

        [EntityPropertyInfo(ColumnName = "ColumnName", DbType = PropertyDbType.String, IsNullable = true, MaxLength = 128, ColumnOrder = 20)]
        public string ColumnName { get; set; }

        [EntityPropertyInfo(ColumnName = "ColumnType", DbType = PropertyDbType.String, IsNullable = false, MaxLength = 128, ColumnOrder = 30)]
        public string ColumnType { get; set; }

        [EntityPropertyInfo(ColumnName = "MaxLength", DbType = PropertyDbType.Integer, IsNullable = false, ColumnOrder = 40)]
        public int MaxLength { get; set; }

        [EntityPropertyInfo(ColumnName = "IsNullable", DbType = PropertyDbType.Boolean, IsNullable = true, ColumnOrder = 50)]
        public bool IsNullable { get; set; }
    }
}
