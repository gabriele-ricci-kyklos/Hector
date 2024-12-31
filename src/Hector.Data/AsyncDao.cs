using Hector.Data.DataMapping;
using Hector.Data.Entities;
using Hector.Data.Queries;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Data
{
    public record AsyncDaoOptions
    (
        string ConnectionString,
        string Schema,
        bool IgnoreEscape
    );

    public record AsyncDaoCommand(string CommandText, SqlParameter[]? Parameters);

    public interface IAsyncDao
    {
        string ConnectionString { get; }

        string Schema { get; }

        IQueryBuilder NewQueryBuilder(string query = "", SqlParameter[]? parameters = null);

        Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;
        Task<int> ExecuteUpsertAsync<T>(IEnumerable<T> items, string? tableName = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;
    }

    public abstract class BaseAsyncDao : IAsyncDao
    {
        protected readonly IAsyncDaoHelper _daoHelper;
        protected readonly AsyncDaoOptions _options;

        public string ConnectionString { get; }

        private string _schema;
        public string Schema
        {
            get
            {
                if (_schema == "-")
                {
                    _schema = GetSchema(_options.Schema);
                }

                return _schema;
            }
        }

        protected BaseAsyncDao(AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper)
        {
            ConnectionString = options.ConnectionString;
            _options = options;
            _daoHelper = asyncDaoHelper;
            _schema = "-";
        }

        protected abstract DbConnection NewDbConnection();

        public IQueryBuilder NewQueryBuilder(string query = "", SqlParameter[]? parameters = null) => new QueryBuilder(_daoHelper, Schema, parameters).SetQuery(query);

        public Task<T?> ExecuteScalarAsync<T>(string query, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteScalarCoreAsync<T>(new AsyncDaoCommand(query, parameters), timeoutInSeconds, cancellationToken);

        public Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteScalarCoreAsync<T>(new AsyncDaoCommand(queryBuilder.Query, queryBuilder.Parameters), timeoutInSeconds, cancellationToken);

        protected async Task<T?> ExecuteScalarCoreAsync<T>(AsyncDaoCommand asyncDaoCommand, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = NewDbConnection();
            using DbCommand command = NewDbCommand(connection, asyncDaoCommand, timeoutInSeconds);

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return (T?)result;
            }
            finally
            {
                connection.Close();
            }
        }

        public Task<int> ExecuteNonQueryAsync(string commandText, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteNonQueryCoreAsync(new AsyncDaoCommand(commandText, parameters), null, timeoutInSeconds, cancellationToken);

        public Task<int> ExecuteNonQueryAsync(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteNonQueryCoreAsync(new AsyncDaoCommand(queryBuilder.Query, queryBuilder.Parameters), null, timeoutInSeconds, cancellationToken);

        protected async Task<int> ExecuteNonQueryCoreAsync(AsyncDaoCommand asyncDaoCommand, Action<DbCommand>? commandFx = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = NewDbConnection();
            using DbCommand command = NewDbCommand(connection, asyncDaoCommand, timeoutInSeconds);

            commandFx?.Invoke(command);

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                int result = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            finally
            {
                connection.Close();
            }
        }

        public Task<T[]> ExecuteSelectQueryAsync<T>(string query, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteSelectQueryCoreAsync<T>(new AsyncDaoCommand(query, parameters), timeoutInSeconds, cancellationToken);

        public Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteSelectQueryCoreAsync<T>(new AsyncDaoCommand(queryBuilder.Query, queryBuilder.Parameters), timeoutInSeconds, cancellationToken);

        public async Task<T[]> ExecuteSelectQueryCoreAsync<T>(AsyncDaoCommand asyncDaoCommand, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = NewDbConnection();
            using DbCommand command = NewDbCommand(connection, asyncDaoCommand, timeoutInSeconds);

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                List<T> results = [];
                GenericDataRecordMapper<T> mapper = new();

                while
                (
                    !cancellationToken.IsCancellationRequested
                    && (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                )
                {
                    T item = mapper.Build(reader);
                    results.Add(item);
                }

                return results.ToArray();
            }
            finally
            {
                connection.Close();
            }
        }

        public abstract Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;
        public abstract Task<int> ExecuteUpsertAsync<T>(IEnumerable<T> items, string? tableName = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;

        private static DbCommand NewDbCommand(DbConnection connection, AsyncDaoCommand asyncDaoCommand, int timeoutInSeconds)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandText = asyncDaoCommand.CommandText;
            command.CommandTimeout = timeoutInSeconds;

            foreach (SqlParameter queryParam in asyncDaoCommand.Parameters ?? [])
            {
                if (queryParam.RawParameter is not null)
                {
                    command.Parameters.Add(queryParam.RawParameter);
                }
                else
                {
                    DbParameter param = command.CreateParameter();
                    param.DbType = BaseAsyncDaoHelper.MapTypeToDbType(queryParam.Type);
                    param.ParameterName = queryParam.Name;
                    param.Value = queryParam.Value;
                    command.Parameters.Add(param);
                }
            }

            return command;
        }

        protected virtual string GetSchema(string rawSchema)
        {
            if (rawSchema.IsNullOrBlankString())
            {
                return string.Empty;
            }

            return $"{rawSchema}.";
        }
    }
}
