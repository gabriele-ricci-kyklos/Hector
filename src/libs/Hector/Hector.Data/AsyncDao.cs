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

        Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int? timeout = null);
        Task<int> ExecuteNonQueryAsync<T>(IQueryBuilder queryBuilder, int? timeout = null);
        Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int? timeout = null);

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

        public async Task<T?> ExecuteScalarAsync<T>(IQueryBuilder queryBuilder, int? timeout = null)
        {
            using DbConnection connection = GetDbConnection();
            using DbCommand command = connection.CreateCommand();

            CreateDbCommand(command, queryBuilder, timeout);

            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                object? result = await command.ExecuteScalarAsync().ConfigureAwait(false);

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

        public Task<int> ExecuteNonQueryAsync<T>(IQueryBuilder queryBuilder, int? timeout = null)
        {
            using DbConnection connection = GetDbConnection();
            using DbCommand command = connection.CreateCommand();

            CreateDbCommand(command, queryBuilder, timeout);

            return ExecuteNonQueryAsync<T>(connection, command);
        }

        internal static async Task<int> ExecuteNonQueryAsync<T>(DbConnection connection, DbCommand command)
        {
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);
                int result = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                return result;
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        public async Task<T[]> ExecuteSelectQueryAsync<T>(IQueryBuilder queryBuilder, int? timeout = null)
        {
            using DbConnection connection = GetDbConnection();
            using DbCommand command = connection.CreateCommand();

            CreateDbCommand(command, queryBuilder, timeout);

            try
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                List<T> results = [];
                GenericDataRecordMapper<T> mapper = new();

                while
                (
                    //!token.IsCancellationRequested &&
                    (await reader.ReadAsync().ConfigureAwait(false))
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

        internal static void CreateDbCommand(DbCommand command, IQueryBuilder builder, int? timeout = null)
        {
            command.CommandText = builder.Query;
            command.CommandTimeout = timeout ?? 30;
            AddParameters(command, builder.Parameters);
        }
    }
}
