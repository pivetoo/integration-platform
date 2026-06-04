using Archon.Application.MultiTenancy;
using Archon.Infrastructure.MultiTenancy;
using IntegrationPlatform.Domain.ValueObjects;

namespace IntegrationPlatform.Api.MultiTenancy
{
    // Resolve o tenant de webhooks ANONIMOS a partir do prefixo de tenant embutido no token
    // (formato PublicLinkToken: "{tenantId}~{secret}"). Guardas: so atua (1) na rota publica de
    // webhook e (2) quando nenhum tenant foi resolvido ainda (request autenticado ou modo
    // single-tenant ja tem tenant -> este middleware nao toca em nada). Token sem prefixo de
    // tenant (legado, sem '~') resulta em no-op, preservando o comportamento atual.
    public sealed class PublicTenantResolutionMiddleware
    {
        private static readonly string[] PublicPrefixes =
        {
            "/api/webhooks/"
        };

        private readonly RequestDelegate next;

        public PublicTenantResolutionMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, ITenantContext tenantContext)
        {
            if (!tenantContext.HasTenant
                && tenantContext is MultiTenantContext multiTenantContext
                && TryGetPublicToken(context.Request.Path, out string token))
            {
                string? tenantId = PublicLinkToken.ExtractTenantId(token);
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    TenantInfo? tenant = await tenantResolver.ResolveAsync(tenantId, context.RequestAborted);
                    if (tenant is not null)
                    {
                        multiTenantContext.SetTenant(tenant);
                    }
                }
            }

            await next(context);
        }

        private static bool TryGetPublicToken(PathString path, out string token)
        {
            token = string.Empty;
            string value = path.Value ?? string.Empty;

            foreach (string prefix in PublicPrefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string rest = value[prefix.Length..];
                    int slash = rest.IndexOf('/');
                    token = slash >= 0 ? rest[..slash] : rest;
                    return token.Length > 0;
                }
            }

            return false;
        }
    }

    public static class PublicTenantResolutionMiddlewareExtensions
    {
        public static IApplicationBuilder UsePublicTenantResolution(this IApplicationBuilder app)
        {
            return app.UseMiddleware<PublicTenantResolutionMiddleware>();
        }
    }
}
