using Hector.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Hector.Data.Oracle
{
    public static class DIExtensions
    {
        public static IServiceCollection AddOracleAsyncDao(this IServiceCollection services) =>
            services
                .AddSingletonOption<AsyncDaoOptions>()
                .AddSingleton<IAsyncDaoHelper, OracleAsyncDaoHelper>()
                .AddSingleton<IAsyncDao, OracleAsyncDao>()
                .AddSingleton<IDbConnectionFactory, OracleDbConnectionFactory>(provider =>
                {
                    AsyncDaoOptions asyncDaoOptions = provider.GetRequiredService<AsyncDaoOptions>();
                    return new OracleDbConnectionFactory(asyncDaoOptions.ConnectionString);
                });
    }
}
