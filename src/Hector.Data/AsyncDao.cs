using Hector.Data.DataMapping;
using Hector.Data.Entities;
using Hector.Data.Queries;
using System;
using System.Collections.Generic;
using System.Data;
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

    public record AsyncDaoCommand
    (
        string CommandText,
        SqlParameter[]? Parameters,
        IsolationLevel IsolationLevel = IsolationLevel.ReadCommitted
    );

    public interface IAsyncDao
    {
        string ConnectionString { get; }

        string Schema { get; }
        IAsyncDaoHelper DaoHelper { get; }

        IQueryBuilder NewQueryBuilder(string query = "", SqlParameter[]? parameters = null);

        Task<T?> ExecuteScalarAsync<T>(string query, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(string query, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<T[]> ExecuteSelectQueryAsync<T>(string query, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;
        Task<int> ExecuteUpsertAsync<T>(IEnumerable<T> items, string? tableName = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;
        Task ExecuteInTransactionAsync(Func<ITransactionalAsyncDao, Task> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    }

    public interface ITransactionalAsyncDao : IAsyncDao
    {
        public DbConnectionContext ConnectionContext { get; }
    }

    public abstract class BaseAsyncDao : IAsyncDao
    {
        protected readonly AsyncDaoOptions _options;
        protected readonly IDbConnectionFactory _connectionFactory;

        public IAsyncDaoHelper DaoHelper { get; }
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

        protected BaseAsyncDao(AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper, IDbConnectionFactory connectionFactory)
        {
            ConnectionString = options.ConnectionString;
            DaoHelper = asyncDaoHelper;
            _options = options;
            _schema = "-";
            _connectionFactory = connectionFactory;
        }

        protected virtual DbConnectionContext NewConnectionContext() => new(_connectionFactory);

        public IQueryBuilder NewQueryBuilder(string query = "", SqlParameter[]? parameters = null) => new QueryBuilder(DaoHelper, Schema, parameters).SetQuery(query);

        public Task<T?> ExecuteScalarAsync<T>(string query, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteScalarCoreAsync<T>(new AsyncDaoCommand(query, parameters), timeoutInSeconds, cancellationToken);

        public Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteScalarCoreAsync<T>(new AsyncDaoCommand(queryBuilder.Query, queryBuilder.Parameters), timeoutInSeconds, cancellationToken);

        protected async Task<T?> ExecuteScalarCoreAsync<T>(AsyncDaoCommand asyncDaoCommand, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnectionContext connectionContext = NewConnectionContext();

            try
            {
                await connectionContext.OpenAsync(cancellationToken).ConfigureAwait(false);

                using DbCommand command = connectionContext.NewDbCommand(connectionContext.DbConnection, asyncDaoCommand, timeoutInSeconds);

                object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return (T?)result;
            }
            finally
            {
                await connectionContext.CloseAsync().ConfigureAwait(false);
            }
        }

        public Task<int> ExecuteNonQueryAsync(string commandText, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteNonQueryCoreAsync(new AsyncDaoCommand(commandText, parameters), null, timeoutInSeconds, cancellationToken);

        public Task<int> ExecuteNonQueryAsync(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteNonQueryCoreAsync(new AsyncDaoCommand(queryBuilder.Query, queryBuilder.Parameters), null, timeoutInSeconds, cancellationToken);

        protected async Task<int> ExecuteNonQueryCoreAsync(AsyncDaoCommand asyncDaoCommand, Action<DbCommand>? commandFx = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnectionContext connectionContext = NewConnectionContext();

            try
            {
                await connectionContext.OpenInTransactionAsync(asyncDaoCommand.IsolationLevel, cancellationToken).ConfigureAwait(false);

                using DbCommand command = connectionContext.NewDbCommand(connectionContext.DbConnection, asyncDaoCommand, timeoutInSeconds);

                commandFx?.Invoke(command);

                int result = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await connectionContext.CommitAsync().ConfigureAwait(false);

                return result;
            }
            catch (Exception)
            {
                await connectionContext.RollbackAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connectionContext.CloseAsync().ConfigureAwait(false);
            }
        }

        public Task<T[]> ExecuteSelectQueryAsync<T>(string query, SqlParameter[]? parameters = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteSelectQueryCoreAsync<T>(new AsyncDaoCommand(query, parameters), timeoutInSeconds, cancellationToken);

        public Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) =>
            ExecuteSelectQueryCoreAsync<T>(new AsyncDaoCommand(queryBuilder.Query, queryBuilder.Parameters), timeoutInSeconds, cancellationToken);

        public async Task<T[]> ExecuteSelectQueryCoreAsync<T>(AsyncDaoCommand asyncDaoCommand, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnectionContext connectionContext = NewConnectionContext();
            using DbCommand command = connectionContext.NewDbCommand(connectionContext.DbConnection, asyncDaoCommand, timeoutInSeconds);

            try
            {
                await connectionContext.OpenAsync(cancellationToken).ConfigureAwait(false);
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
                await connectionContext.CloseAsync().ConfigureAwait(false);
            }
        }

        public abstract Task<int> ExecuteBulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;
        public abstract Task<int> ExecuteUpsertAsync<T>(IEnumerable<T> items, string? tableName = null, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;

        public abstract ITransactionalAsyncDao NewTransactionalAsyncDao(DbConnectionContext connectionContext);

        public async Task ExecuteInTransactionAsync(Func<ITransactionalAsyncDao, Task> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
        {
            using DbConnectionContext connectionContext = NewConnectionContext();

            try
            {
                await connectionContext.OpenInTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

                ITransactionalAsyncDao transactionalAsyncDao = NewTransactionalAsyncDao(connectionContext);
                await action(transactionalAsyncDao).ConfigureAwait(false);

                await connectionContext.CommitAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                await connectionContext.RollbackAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await connectionContext.CloseAsync().ConfigureAwait(false);
            }
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
