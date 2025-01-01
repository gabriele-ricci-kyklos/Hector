using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Data
{
    public record DbConnectionModel(DbConnection DbConnection, DbTransaction? DbTransaction) : IDisposable
    {
        public bool IsTransactional => DbTransaction != null;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async ValueTask CommitAsync()
        {
#if NETSTANDARD2_1_OR_GREATER
            await (DbTransaction?.CommitAsync() ?? Task.CompletedTask).ConfigureAwait(false);
#else
            DbTransaction?.Commit();
#endif
        }

        public async ValueTask RollbackAsync()
        {
#if NETSTANDARD2_1_OR_GREATER
            await (DbTransaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
#else
            DbTransaction?.Rollback();
#endif
        }

        public async ValueTask CloseAsync()
        {
#if NETSTANDARD2_1_OR_GREATER
            await DbConnection.CloseAsync().ConfigureAwait(false);
#else
            DbConnection.Close();
#endif
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public void Dispose()
        {
            DbConnection.Dispose();
            DbTransaction?.Dispose();
        }
    }

    public interface IDbConnectionFactory
    {
        DbConnection NewDbConnection();
        Task<DbConnectionModel> OpenAsync
        (
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool startTransaction = true,
            CancellationToken cancellationToken = default
        );
    }

    public abstract class BaseDbConnectionFactory(string connectionString) : IDbConnectionFactory
    {
        public string ConnectionString { get; } = connectionString;

        public abstract DbConnection NewDbConnection();

        public async Task<DbConnectionModel> OpenAsync
        (
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool startTransaction = true,
            CancellationToken cancellationToken = default
        )
        {
            DbConnection connection = NewDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_1_OR_GREATER
            DbTransaction? transaction = startTransaction ? await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false) : null;
#else
            DbTransaction? transaction = startTransaction ? connection.BeginTransaction(isolationLevel) : null;
#endif

            return new DbConnectionModel(connection, transaction);
        }
    }
}
