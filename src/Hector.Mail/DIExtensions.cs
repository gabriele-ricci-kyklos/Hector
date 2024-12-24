using Microsoft.Extensions.DependencyInjection;

namespace Hector.Mail
{
    public static class DIExtensions
    {
        public static IServiceCollection AddMailClient(this IServiceCollection services) =>
            services
                .AddSingleton<IMailClient, MailKitMailClient>();
    }
}
