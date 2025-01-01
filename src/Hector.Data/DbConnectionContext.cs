using Hector.Data.Queries;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Hector.Data
{
    public enum TransactionStatus
    {
        NotStarted,
        InProgress,
        Committed,
        RolledBack
    }

    public class DbConnectionContext : IDisposable
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly DbConnectionContext? _parentContext;

        private TransactionStatus _transactionStatus;
        private DbConnectionModel? _connectionModel;

        public int NestingLevel { get; }
        public TransactionStatus TransactionStatus => _parentContext?.TransactionStatus ?? _transactionStatus;

        public DbConnection DbConnection =>
            _parentContext?.DbConnection
            ?? _connectionModel?.DbConnection
            ?? throw ThrowConnectionError();

        public DbTransaction? DbTransaction => _parentContext?.DbTransaction ?? _connectionModel?.DbTransaction;

        public DbConnectionContext(IDbConnectionFactory connectionFactory, DbConnectionContext? parentContext = null)
        {
            _connectionFactory = connectionFactory;
            _parentContext = parentContext;
            NestingLevel = (parentContext?.NestingLevel ?? -1) + 1;
            _transactionStatus = TransactionStatus.NotStarted;
        }

        public ValueTask OpenAsync(CancellationToken cancellationToken = default) =>
            OpenAsync(IsolationLevel.ReadCommitted, false, cancellationToken);

        public ValueTask OpenInTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default) =>
            OpenAsync(isolationLevel, true, cancellationToken);

        private async ValueTask OpenAsync
        (
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool startTransaction = true,
            CancellationToken cancellationToken = default
        )
        {
            if (_parentContext is not null)
            {
                return;
            }

            _connectionModel =
                await _connectionFactory
                    .OpenAsync(isolationLevel, startTransaction, cancellationToken)
                    .ConfigureAwait(false);

            _transactionStatus = _connectionModel.IsTransactional ? TransactionStatus.InProgress : TransactionStatus.NotStarted;
        }

        public async ValueTask CommitAsync()
        {
            if (_parentContext is not null)
            {
                return;
            }

            if (_connectionModel is null)
            {
                throw ThrowConnectionError();
            }

            if (_transactionStatus == TransactionStatus.InProgress && NestingLevel == 0)
            {
                _transactionStatus = TransactionStatus.Committed;
                await _connectionModel.CommitAsync().ConfigureAwait(false);
            }
        }

        public async ValueTask RollbackAsync()
        {
            if (_parentContext is not null)
            {
                return;
            }

            if (_connectionModel is null)
            {
                throw ThrowConnectionError();
            }

            if (_transactionStatus == TransactionStatus.InProgress && NestingLevel == 0)
            {
                await _connectionModel.RollbackAsync().ConfigureAwait(false);
            }

            if (_transactionStatus != TransactionStatus.NotStarted)
            {
                _transactionStatus = TransactionStatus.RolledBack;
            }
        }

        public async ValueTask CloseAsync()
        {
            if (_parentContext is not null || _connectionModel is null)
            {
                return;
            }

            if (TransactionStatus == TransactionStatus.InProgress)
            {
                await _connectionModel.CommitAsync().ConfigureAwait(false);
            }

            await _connectionModel.CloseAsync().ConfigureAwait(false);
        }

        public DbCommand NewDbCommand(DbConnection connection, AsyncDaoCommand asyncDaoCommand, int timeoutInSeconds)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.Transaction = DbTransaction;
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

        public void Dispose()
        {
            if (_parentContext is not null)
            {
                return;
            }

            if (_connectionModel is not null && _transactionStatus == TransactionStatus.InProgress)
            {
                ValueTask rollbackValueTask = RollbackAsync();
                if (!rollbackValueTask.IsCompleted)
                {
                    rollbackValueTask.GetAwaiter().GetResult();
                }

                ValueTask closeValueTask = CloseAsync();
                if (!closeValueTask.IsCompleted)
                {
                    closeValueTask.GetAwaiter().GetResult();
                }
            }

            _parentContext?.Dispose();
            _connectionModel?.Dispose();
        }

        private Exception ThrowConnectionError() => new Exception($"Please create and open the connection first with {nameof(OpenAsync)}");
    }
}
