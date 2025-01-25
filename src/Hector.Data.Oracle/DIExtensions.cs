using Hector.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Hector.Data.Oracle
{
    public static class DIExtensions
    {
        public static IServiceCollection AddOracleAsyncDao(this IServiceCollection services, AsyncDaoOptions? options = null)
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
                    .AddSingleton<IAsyncDaoHelper, OracleAsyncDaoHelper>
                    (provider =>
                    {
                        AsyncDaoOptions options = provider.GetRequiredService<AsyncDaoOptions>();
                        return new OracleAsyncDaoHelper(options.IgnoreEscape);
                    })
                    .AddSingleton<IDbConnectionFactory, OracleDbConnectionFactory>(provider =>
                    {
                        AsyncDaoOptions asyncDaoOptions = provider.GetRequiredService<AsyncDaoOptions>();
                        return new OracleDbConnectionFactory(asyncDaoOptions.ConnectionString);
                    })
                    .AddSingleton<IAsyncDao, OracleAsyncDao>();
        }
    }
}
