using Archon.Application.MultiTenancy;
using Archon.Infrastructure.MultiTenancy;
using IntegrationPlatform.Application.Services;
using System.Text.Json;

namespace IntegrationPlatform.Infrastructure.Services
{
    public sealed class WebhookReceiverService : IWebhookReceiverService
    {
        private readonly ITenantResolver tenantResolver;
        private readonly ITenantContext tenantContext;

        public WebhookReceiverService(ITenantResolver tenantResolver, ITenantContext tenantContext)
        {
            this.tenantResolver = tenantResolver;
            this.tenantContext = tenantContext;
        }

        public async Task<bool> ResolveTenantAsync(string tenantId, CancellationToken cancellationToken)
        {
            TenantInfo? tenant = await tenantResolver.ResolveAsync(tenantId, cancellationToken);
            if (tenant is null)
            {
                return false;
            }

            if (tenantContext is MultiTenantContext multiTenantContext)
            {
                multiTenantContext.SetTenant(tenant);
            }

            return true;
        }

        public async Task<string> ReadBodyAsync(Stream body, CancellationToken cancellationToken)
        {
            using StreamReader reader = new(body);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        public Dictionary<string, object> ParseBody(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return [];
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(rawBody);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(rawBody) ?? [];
                }
            }
            catch (JsonException)
            {
            }

            return new Dictionary<string, object> { ["payload"] = rawBody };
        }
    }
}
