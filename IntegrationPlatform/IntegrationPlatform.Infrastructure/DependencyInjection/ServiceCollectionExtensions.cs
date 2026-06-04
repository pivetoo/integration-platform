using Archon.Infrastructure.DependencyInjection;
using Archon.Infrastructure.Migrations;
using Archon.Infrastructure.MultiTenancy;
using IntegrationPlatform.Application.Services;
using IntegrationPlatform.Infrastructure.BackgroundJobs;
using IntegrationPlatform.Infrastructure.Services.ExecutionEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIntegrationPlatformInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddArchonPersistence(configuration, typeof(ServiceCollectionExtensions).Assembly);
            services.AddHttpClient();
            services.RunMigrations(
                configuration,
                GetMigrationSchema(configuration),
                typeof(DatabaseMigrator).Assembly,
                typeof(ServiceCollectionExtensions).Assembly);
            services.AddServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
            services.AddScoped<IServiceCallbackDispatcher, ServiceCallbackDispatcher>();
            services.AddIntegrationPlatformBackgroundJobs(configuration);

            return services;
        }

        private static string GetMigrationSchema(IConfiguration configuration)
        {
            TenantDatabaseOptions tenantDatabaseOptions = new();
            configuration.Bind(tenantDatabaseOptions);

            string? schema = tenantDatabaseOptions.TenantDatabases
                .Select(item => item.Value.Schema)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

            return schema ?? "public";
        }
    }
}
