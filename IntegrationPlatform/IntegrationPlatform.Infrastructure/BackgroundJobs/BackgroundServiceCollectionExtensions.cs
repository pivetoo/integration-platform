using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.Infrastructure.BackgroundJobs
{
    public static class BackgroundServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationPlatformBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BackgroundJobOptions>(configuration.GetSection("BackgroundJobs"));
            services.AddSingleton<TenantJobRunner>();

            return services;
        }
    }
}
