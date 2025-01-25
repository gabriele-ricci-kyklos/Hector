using System;

namespace Hector.Data
{
    public static class AsyncDaoFactory
    {
        public static IAsyncDao CreateAsyncDao(string providerName, AsyncDaoOptions options)
        {
            string assembyName = $"Hector.Data.{providerName}";
            string asyncDaoTypeName = $"{assembyName}.{providerName}AsyncDao, {assembyName}";
            string asyncDaoHelperTypeName = $"{assembyName}.{providerName}AsyncDaoHelper, {assembyName}";
            string dbConnectionFactoryTypeName = $"{assembyName}.{providerName}DbConnectionFactory, {assembyName}";

            Type asyncDaoType =
                Type.GetType(asyncDaoTypeName)
                ?? throw new TypeLoadException($"Unable to load the async dao type for the provider {providerName}");

            Type asyncDaoHelperType =
                Type.GetType(asyncDaoHelperTypeName)
                ?? throw new TypeLoadException($"Unable to load the async dao helper type for the provider {providerName}");

            Type dbConnectionFactoryType =
                Type.GetType(dbConnectionFactoryTypeName)
                ?? throw new TypeLoadException($"Unable to load the connection factory type for the provider {providerName}");

            IAsyncDaoHelper daoHelper =
                (IAsyncDaoHelper)Activator
                .CreateInstance
                (
                    asyncDaoHelperType,
                    args: options.IgnoreEscape
                );

            IDbConnectionFactory connectionFactory =
                (IDbConnectionFactory)Activator
                .CreateInstance
                (
                    dbConnectionFactoryType,
                    options.ConnectionString
                );

            return
                (IAsyncDao)Activator
                .CreateInstance
                (
                    asyncDaoType,
                    options,
                    daoHelper,
                    connectionFactory
                );
        }
    }
}
