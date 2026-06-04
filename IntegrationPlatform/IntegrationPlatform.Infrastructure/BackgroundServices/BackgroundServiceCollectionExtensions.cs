using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.Infrastructure.BackgroundServices
{
    public static class BackgroundServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationPlatformBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BackgroundJobOptions>(configuration.GetSection("BackgroundJobs"));
            services.AddSingleton<TenantJobRunner>();
            services.AddHostedService<PipelineRoutineSchedulerService>();
            services.AddHostedService<QueueWorkerService>();

            return services;
        }
    }
}
