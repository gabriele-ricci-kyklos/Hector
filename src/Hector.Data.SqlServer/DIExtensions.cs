using Hector.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Hector.Data.SqlServer
{
    public static class DIExtensions
    {
        public static IServiceCollection AddSqlServerAsyncDao(this IServiceCollection services, AsyncDaoOptions? options = null)
        {
            if (options is not null)
            {
                services = services.AddSingleton(options);
            }
            else
            {
                services = services.AddSingletonOption<AsyncDaoOptions>();
            }

            return
                services
                    .AddSingleton<IAsyncDaoHelper, SqlServerAsyncDaoHelper>
                    (provider =>
                    {
                        AsyncDaoOptions options = provider.GetRequiredService<AsyncDaoOptions>();
                        return new SqlServerAsyncDaoHelper(options.IgnoreEscape);
                    })
                    .AddSingleton<IDbConnectionFactory, SqlServerDbConnectionFactory>(provider =>
                    {
                        AsyncDaoOptions asyncDaoOptions = provider.GetRequiredService<AsyncDaoOptions>();
                        return new SqlServerDbConnectionFactory(asyncDaoOptions.ConnectionString);
                    })
                    .AddSingleton<IAsyncDao, SqlServerAsyncDao>();
        }
    }
}
