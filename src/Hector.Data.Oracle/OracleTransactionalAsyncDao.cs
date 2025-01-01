namespace Hector.Data.Oracle
{
    public class OracleTransactionalAsyncDao : OracleAsyncDao, ITransactionalAsyncDao
    {
        public DbConnectionContext ConnectionContext { get; }

        public OracleTransactionalAsyncDao(DbConnectionContext parentConnectionContext, AsyncDaoOptions options, IAsyncDaoHelper asyncDaoHelper, IDbConnectionFactory connectionFactory)
            : base(options, asyncDaoHelper, connectionFactory)
        {
            ConnectionContext = new DbConnectionContext(connectionFactory, parentConnectionContext);
        }

        protected override DbConnectionContext NewConnectionContext() => ConnectionContext;
    }
}
