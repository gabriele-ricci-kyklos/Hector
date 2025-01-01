using Hector.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Hector.Data.SqlServer
{
    public static class DIExtensions
    {
        public static IServiceCollection AddSqlServerAsyncDao(this IServiceCollection services) =>
            services
                .AddSingletonOption<AsyncDaoOptions>()
                .AddSingleton<IAsyncDaoHelper, SqlServerAsyncDaoHelper>()
                .AddSingleton<IAsyncDao, SqlServerAsyncDao>()
                .AddSingleton<IDbConnectionFactory, SqlServerDbConnectionFactory>(provider =>
                {
                    AsyncDaoOptions asyncDaoOptions = provider.GetRequiredService<AsyncDaoOptions>();
                    return new SqlServerDbConnectionFactory(asyncDaoOptions.ConnectionString);
                });
    }
}
