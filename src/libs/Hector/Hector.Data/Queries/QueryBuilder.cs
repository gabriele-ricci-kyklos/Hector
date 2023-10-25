using System;
using System.Collections.Generic;

namespace Hector.Data.Queries
{
    public interface IQueryBuilder
    {
        public string Query { get; }
        public SqlParameter[] Parameters { get; }
    }

    public class QueryBuilder : IQueryBuilder
    {
        private readonly IAsyncDaoHelper _asyncDaoHelper;
        private readonly List<SqlParameter> _parameters;

        internal QueryBuilder(IAsyncDaoHelper asyncDaoHelper)
        {
            _asyncDaoHelper = asyncDaoHelper;
            _parameters = new();
        }

        public string Query => PrepareQuery();

        public SqlParameter[] Parameters => _parameters.ToArray();

        private string PrepareQuery()
        {
            throw new NotImplementedException();
        }
    }
}
