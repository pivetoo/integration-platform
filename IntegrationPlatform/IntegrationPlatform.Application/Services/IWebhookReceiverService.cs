namespace IntegrationPlatform.Application.Services
{
    public interface IWebhookReceiverService
    {
        Task<bool> ResolveTenantAsync(string tenantId, CancellationToken cancellationToken);
        Task<string> ReadBodyAsync(Stream body, CancellationToken cancellationToken);
        Dictionary<string, object> ParseBody(string rawBody);
    }
}
