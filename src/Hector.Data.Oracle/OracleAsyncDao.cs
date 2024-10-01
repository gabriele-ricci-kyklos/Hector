using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
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

        public override Task ExecuteBulkCopyAsync<T>(IEnumerable<T> items, int batchSize = 0, int timeoutInSeconds = 30, CancellationToken cancellationToken = default)
        {
            //Va creato un query builder con una insert into con i nomi campi e parametri dell'entità
            //Va creata forse una logica common per realizzarlo
            //Poi settare il paramentro ArrayBindCount dentro OracleCommand con il count delle entità da inserire
            //Ed eseguire con ExecuteNonQueryAsync

            throw new NotImplementedException();
        }
    }
}
