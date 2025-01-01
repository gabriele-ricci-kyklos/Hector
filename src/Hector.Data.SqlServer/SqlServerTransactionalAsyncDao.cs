namespace Hector.Data.SqlServer
{
    public class SqlServerTransactionalAsyncDao : SqlServerAsyncDao, ITransactionalAsyncDao
    {
        public DbConnectionContext ConnectionContext { get; }

        public SqlServerTransactionalAsyncDao(DbConnectionContext parentConnectionContext, AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper, IDbConnectionFactory connectionFactory)
            : base(options, asyncDaoHelper, connectionFactory)
        {
            ConnectionContext = new DbConnectionContext(connectionFactory, parentConnectionContext);
        }

        protected override DbConnectionContext NewConnectionContext() => ConnectionContext;
    }
}
