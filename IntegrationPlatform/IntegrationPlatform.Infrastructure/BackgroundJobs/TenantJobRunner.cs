using System.Globalization;
using Archon.Application.MultiTenancy;
using Archon.Infrastructure.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Executa uma acao uma vez por tenant configurado, criando um escopo DI proprio e
    /// resolvendo o contexto de tenant ANTES de qualquer servico/DbContext ser usado.
    /// Necessario porque, fora de um request HTTP, ninguem seta o tenant e o DbContext
    /// cairia no fallback do primeiro tenant. A lista de tenants vem do `TenantDatabaseOptions`,
    /// populado no startup pelo `TenantCatalogBootstrap` do Archon (que busca os tenants do
    /// IdentityManagement via `ListByApplication` e injeta na configuracao).
    /// </summary>
    public sealed class TenantJobRunner
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly TenantDatabaseOptions tenantDatabaseOptions;
        private readonly ILogger<TenantJobRunner> logger;

        public TenantJobRunner(IServiceScopeFactory scopeFactory, TenantDatabaseOptions tenantDatabaseOptions, ILogger<TenantJobRunner> logger)
        {
            this.scopeFactory = scopeFactory;
            this.tenantDatabaseOptions = tenantDatabaseOptions;
            this.logger = logger;
        }

        private static readonly CultureInfo DefaultCulture = new("pt-BR");

        public async Task RunForAllTenants(Func<IServiceProvider, CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            // Fora de um request HTTP nao ha RequestLocalization, entao a thread roda em cultura
            // invariante e o IStringLocalizer cai no recurso neutro (inexistente), devolvendo a
            // chave crua nos logs. Fixar a cultura padrao garante a traducao das mensagens de execucao.
            CultureInfo.CurrentCulture = DefaultCulture;
            CultureInfo.CurrentUICulture = DefaultCulture;

            foreach (KeyValuePair<string, TenantDatabaseOption> entry in tenantDatabaseOptions.TenantDatabases)
            {
                if (string.IsNullOrWhiteSpace(entry.Value.ConnectionString))
                {
                    continue;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                using IServiceScope scope = scopeFactory.CreateScope();

                try
                {
                    MultiTenantContext tenantContext = scope.ServiceProvider.GetRequiredService<MultiTenantContext>();
                    tenantContext.SetTenant(new TenantInfo
                    {
                        TenantId = entry.Key,
                        CompanyName = entry.Value.CompanyName,
                        ApplicationId = entry.Value.ApplicationId,
                        ConnectionString = entry.Value.ConnectionString,
                        Schema = string.IsNullOrWhiteSpace(entry.Value.Schema) ? "public" : entry.Value.Schema,
                        DatabaseProvider = entry.Value.GetDatabaseProvider(),
                        ApiKey = entry.Value.ApiKey
                    });

                    await action(scope.ServiceProvider, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Background job failed for tenant {Tenant}.", entry.Key);
                }
            }
        }
    }
}
