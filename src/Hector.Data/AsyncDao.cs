﻿using Hector.Data.DataMapping;
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

    public interface IAsyncDao
    {
        string ConnectionString { get; }

        string Schema { get; }

        IQueryBuilder NewQueryBuilder(string query = "");

        Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<int> ExecuteNonQueryAsync(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);
        Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default);

        Task ExecuteBulkCopyAsync<T>(IEnumerable<T> items, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;
    }

    public abstract class BaseAsyncDao : IAsyncDao
    {
        private readonly IAsyncDaoHelper _daoHelper;

        public string ConnectionString { get; }

        public string Schema { get; }

        protected BaseAsyncDao(AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper)
        {
            ConnectionString = options.ConnectionString;
            Schema = options.Schema;
            _daoHelper = asyncDaoHelper;
        }

        protected abstract DbConnection GetDbConnection();

        public IQueryBuilder NewQueryBuilder(string query = "") => new QueryBuilder(_daoHelper, Schema).SetQuery(query);

        public async Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = GetDbConnection();
            using DbCommand command = connection.CreateCommand();

            CreateDbCommand(command, queryBuilder, timeoutInSeconds);

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    T? value = default;
                    return value;
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"Unable to cast return value to type {typeof(T?).FullName}", ex);
                }
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        public Task<int> ExecuteNonQueryAsync(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = GetDbConnection();
            using DbCommand command = connection.CreateCommand();

            CreateDbCommand(command, queryBuilder, timeoutInSeconds);

            return ExecuteNonQueryAsync(connection, command, cancellationToken);
        }

        internal static async Task<int> ExecuteNonQueryAsync(DbConnection connection, DbCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                int result = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        public async Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = GetDbConnection();
            using DbCommand command = connection.CreateCommand();

            CreateDbCommand(command, queryBuilder, timeoutInSeconds);

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
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }


        public abstract Task ExecuteBulkCopyAsync<T>(IEnumerable<T> items, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default) where T : IBaseEntity;


        private static void AddParameters(DbCommand command, SqlParameter[] parameters)
        {
            foreach (SqlParameter queryParam in parameters)
            {
                DbParameter param = command.CreateParameter();
                param.DbType = DbTypeMapper.MapTypeToDbType(queryParam.Type);
                param.ParameterName = queryParam.Name;
                param.Value = queryParam.Value;
                command.Parameters.Add(param);
            }
        }

        internal static void CreateDbCommand(DbCommand command, IQueryBuilder builder, int timeoutInSeconds = 30)
        {
            command.CommandText = builder.Query;
            command.CommandTimeout = timeoutInSeconds;
            AddParameters(command, builder.Parameters);
        }
    }
}
