using System.Globalization;
using Archon.Application.MultiTenancy;
using Archon.Core.ValueObjects;
using Archon.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Executa uma acao uma vez por tenant configurado, criando um escopo DI proprio e
    /// resolvendo o contexto de tenant ANTES de qualquer servico/DbContext ser usado.
    /// Necessario porque, fora de um request HTTP, ninguem seta o tenant e o DbContext
    /// cairia no fallback do primeiro tenant. A lista de tenants vem do
    /// `TenantCatalogBootstrap.RefreshIfStale` do Archon, que reconsulta o IdentityManagement quando
    /// o cache envelhece - NAO do `TenantDatabaseOptions` congelado no boot, porque tenant criado
    /// depois do processo subir ficava invisivel pros jobs de fundo ate reiniciar o servico (bug
    /// encontrado em 2026-08-24: o queue worker nunca processava a fila de um tenant provisionado
    /// horas depois do processo ja estar de pe).
    /// </summary>
    public sealed class TenantJobRunner
    {
        // Tenant novo fica visivel em ate esse tempo, sem exigir restart do processo. Baixo o
        // suficiente pra nao atrasar onboarding, alto o suficiente pra nao bater no IdentityManagement
        // a cada tick de job de 5 em 5 segundos.
        private static readonly TimeSpan TenantCatalogMaxAge = TimeSpan.FromMinutes(2);
        private static readonly CultureInfo DefaultCulture = new("pt-BR");

        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private readonly ILogger<TenantJobRunner> logger;

        public TenantJobRunner(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<TenantJobRunner> logger)
        {
            this.scopeFactory = scopeFactory;
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task RunForAllTenants(Func<IServiceProvider, CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            // Fora de um request HTTP nao ha RequestLocalization, entao a thread roda em cultura
            // invariante e o IStringLocalizer cai no recurso neutro (inexistente), devolvendo a
            // chave crua nos logs. Fixar a cultura padrao garante a traducao das mensagens de execucao.
            CultureInfo.CurrentCulture = DefaultCulture;
            CultureInfo.CurrentUICulture = DefaultCulture;

            IReadOnlyList<BootstrapTenant> tenants = TenantCatalogBootstrap.RefreshIfStale(configuration, TenantCatalogMaxAge);

            foreach (BootstrapTenant tenant in tenants)
            {
                if (string.IsNullOrWhiteSpace(tenant.ConnectionString))
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
                        TenantId = tenant.TenantId,
                        CompanyName = tenant.CompanyName,
                        ApplicationId = tenant.ApplicationId,
                        ConnectionString = tenant.ConnectionString,
                        Schema = string.IsNullOrWhiteSpace(tenant.Schema) ? "public" : tenant.Schema,
                        DatabaseProvider = (DatabaseProvider)tenant.DatabaseProvider,
                        ApiKey = tenant.ApiKey
                    });

                    await action(scope.ServiceProvider, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Background job failed for tenant {Tenant}.", tenant.TenantId);
                }
            }
        }
    }
}
